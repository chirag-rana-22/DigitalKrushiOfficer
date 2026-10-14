using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace Digital_Krushi_Officer.Services;

// Public AGMARKNET reports used by its own website. No key, login or captcha bypass.
// Weekly values are labelled as reported prices, never invented min/modal/max prices.
public sealed class OfficialMarketPriceService(IHttpClientFactory clients, IMemoryCache cache) {
    public const string Source = "https://agmarknet.gov.in/home";
    private const string Api = "https://api.agmarknet.gov.in/v1/";
    private record Named(int Id, string Name, int StateId = 0, int DistrictId = 0);
    private record Filters(List<Named> Crops, List<Named> States, List<Named> Districts, List<Named> Markets);
    private record Report(string Variety, string Market, DateOnly Date, decimal Price);

    private async Task<JsonDocument?> Get(string path, CancellationToken ct) {
        using var request = new HttpRequestMessage(HttpMethod.Get, Api + path);
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.Referrer = new Uri(Source);
        request.Headers.TryAddWithoutValidation("Origin", "https://agmarknet.gov.in");
        request.Headers.UserAgent.ParseAdd("DigitalKrushiOfficer/1.0");
        using var response = await clients.CreateClient("market-prices").SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) return null;
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
    }

    public async Task<string?> AnswerAsync(MarketPriceRequest r, string language, CancellationToken ct = default) {
        string Local(string en,string hi,string gu) => ReminderLanguage.Text(language,en,hi,gu);
        var key = "official-mandi:" + JsonSerializer.Serialize(r).ToLowerInvariant();
        if (cache.TryGetValue(key, out List<Report>? saved) && saved is { Count: > 0 }) return Format(saved);
        if (cache.TryGetValue("official-mandi-backoff", out bool unavailable) && unavailable) return null;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(8));
        try {
            if (!cache.TryGetValue("official-mandi-filters", out Filters? filters)) {
                using var doc = await Get("daily-price-arrival/filters", timeout.Token);
                if (doc is null || !doc.RootElement.TryGetProperty("data", out var data)) return Failed();
                List<Named> Read(string list,string id,string name,bool location=false) {
                    if (!data.TryGetProperty(list,out var array) || array.ValueKind != JsonValueKind.Array) return [];
                    int Number(JsonElement e,string field) => e.TryGetProperty(field,out var v) && v.ValueKind==JsonValueKind.Number && v.TryGetInt32(out var n) ? n : 0;
                    return array.EnumerateArray().Select(e=>new Named(Number(e,id),e.GetProperty(name).GetString()??"",location?Number(e,"state_id"):0,location?Number(e,"district_id"):0)).Where(x=>x.Id>0 && x.Name.Length>0).ToList();
                }
                filters = new(Read("cmdt_data","cmdt_id","cmdt_name"),Read("state_data","state_id","state_name"),Read("district_data","id","district_name",true),Read("market_data","id","mkt_name",true));
                if (filters.Crops.Count==0 || filters.States.Count==0 || filters.Markets.Count==0) return Failed();
                cache.Set("official-mandi-filters",filters,TimeSpan.FromHours(6));
            }
            var crop=filters!.Crops.FirstOrDefault(x=>x.Name.Equals(r.Commodity,StringComparison.OrdinalIgnoreCase));
            var state=filters.States.FirstOrDefault(x=>x.Name.Equals(r.State,StringComparison.OrdinalIgnoreCase));
            var district=filters.Districts.FirstOrDefault(x=>x.StateId==state?.Id && x.Name.Equals(r.District,StringComparison.OrdinalIgnoreCase));
            if (crop is null || state is null || (r.District.Length>0 && district is null)) return null;
            string MarketName(string value)=>value.EndsWith(" APMC",StringComparison.OrdinalIgnoreCase)?value[..^5].Trim():value.Trim();
            var matches=filters.Markets.Where(x=>x.StateId==state.Id && (district is null || x.DistrictId==district.Id)).ToList();
            if (r.Market.Length>0) matches=matches.Where(x=>MarketName(x.Name).Equals(MarketName(r.Market),StringComparison.OrdinalIgnoreCase)).ToList();
            // A district name also names its central APMC. Clearly display that APMC,
            // rather than silently mixing unrelated neighbouring market prices.
            if (r.Market.Length==0 && district is not null) {
                var central=matches.Where(x=>MarketName(x.Name).Equals(district.Name,StringComparison.OrdinalIgnoreCase)).ToList();
                if (central.Count>0) matches=central;
            }
            if (matches.Count==0 || matches.Count>4) return null; // Do not silently truncate district coverage.
            var today=DateOnly.FromDateTime(DateTime.UtcNow.AddHours(5.5));
            var rows=new List<Report>();
            foreach (var market in matches) {
                using var doc=await Get($"prices-and-arrivals/commodity-price/lastweek?marketId={market.Id}&stateId={state.Id}&commodityId={crop.Id}&includeExcel=false",timeout.Token);
                if (doc is null || !doc.RootElement.TryGetProperty("success",out var success) || success.ValueKind!=JsonValueKind.True) continue;
                if (!doc.RootElement.TryGetProperty("title",out var title) || !title.ToString().Contains($"Market: {market.Name}, State/UT: {state.Name}",StringComparison.OrdinalIgnoreCase)) continue;
                if (!doc.RootElement.TryGetProperty("data",out var data) || data.ValueKind!=JsonValueKind.Array) continue;
                foreach (var row in data.EnumerateArray()) {
                    if (!row.TryGetProperty("unitOfPrice",out var unit) || unit.GetString()!="Rs./Quintal") continue;
                    if (!row.TryGetProperty("variety",out var variety) || !variety.ToString().StartsWith(crop.Name+" - ",StringComparison.OrdinalIgnoreCase)) continue;
                    foreach (var value in row.EnumerateObject()) {
                        if (!DateOnly.TryParseExact(value.Name,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var date) || date>today) continue;
                        if (!decimal.TryParse(value.Value.ToString(),NumberStyles.Number,CultureInfo.InvariantCulture,out var price) || price<=0) continue;
                        rows.Add(new(variety.ToString(),market.Name,date,price));
                    }
                }
            }
            if (rows.Count==0) return null;
            cache.Set(key,rows,TimeSpan.FromMinutes(5));
            return Format(rows);
        } catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException or KeyNotFoundException) { return Failed(); }

        string? Failed() { cache.Set("official-mandi-backoff",true,TimeSpan.FromSeconds(30)); return null; }
        string Format(List<Report> rows) {
            var latest=rows.Max(x=>x.Date);
            var title=Local("Official AGMARKNET reported wholesale price (₹ per quintal; 1 quintal = 100 kg).","आधिकारिक AGMARKNET में दर्ज थोक भाव (₹ प्रति क्विंटल; 1 क्विंटल = 100 किलो)।","સત્તાવાર AGMARKNETમાં નોંધાયેલ જથ્થાબંધ ભાવ (₹ પ્રતિ ક્વિન્ટલ; 1 ક્વિન્ટલ = 100 કિલો)." );
            if (latest<DateOnly.FromDateTime(DateTime.UtcNow.AddHours(5.5))) title+="\n"+Local("Older report — these are NOT today's confirmed prices.","पुरानी report — ये आज के पुष्टि किए गए भाव नहीं हैं।","જૂનો અહેવાલ — આ આજના પુષ્ટિ કરેલા ભાવ નથી.");
            var entries=rows.Where(x=>x.Date==latest).OrderBy(x=>x.Market).ThenBy(x=>x.Variety).Take(8).Select(x=>$"{x.Variety}\n{string.Join(", ",new[]{x.Market,r.District,r.State}.Where(v=>v.Length>0))} · {x.Date.ToString("dd/MM/yyyy",CultureInfo.InvariantCulture)}\n{Local("Reported price","दर्ज भाव","નોંધાયેલ ભાવ")}: ₹{x.Price.ToString("0.##",CultureInfo.InvariantCulture)} / {Local("quintal","क्विंटल","ક્વિન્ટલ")}");
            return title+"\n\n"+string.Join("\n\n",entries)+"\n\n"+Local("Source: AGMARKNET official weekly report. Latest available reported date shown. This report does not supply separate minimum, maximum or modal fields; the price is not a guaranteed selling price.","स्रोत: AGMARKNET की आधिकारिक साप्ताहिक रिपोर्ट। उपलब्ध नवीनतम रिपोर्ट की तारीख दिखाई गई है। इस report में अलग Min/Max/Modal fields नहीं हैं; बिक्री भाव की गारंटी नहीं है।","સ્ત્રોત: AGMARKNETનો સત્તાવાર સાપ્તાહિક અહેવાલ. ઉપલબ્ધ તાજેતરના અહેવાલની તારીખ બતાવી છે. આ અહેવાલમાં અલગ Min/Max/Modal fields નથી; વેચાણ ભાવની ખાતરી નથી.")+"\n"+Source;
        }
    }
}

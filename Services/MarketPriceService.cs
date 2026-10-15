using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
namespace Digital_Krushi_Officer.Services;
public record MarketPriceRequest(string Commodity, string State, string District, string Market);
public interface IMarketPriceService { Task<string> AnswerAsync(MarketPriceRequest request, string language, CancellationToken ct = default); }
public class MarketPriceService(IConfiguration config, IHttpClientFactory clients, IMemoryCache cache, ILogger<MarketPriceService>? logger = null) : IMarketPriceService {
    public const string Source = "https://www.data.gov.in/resource/current-daily-price-various-commodities-various-markets-mandi";
    private record Price(string Commodity,string State,string District,string Market,string Variety,DateOnly Date,decimal Min,decimal Max,decimal Modal);
    public async Task<string> AnswerAsync(MarketPriceRequest request,string language,CancellationToken ct=default) {
        string Local(string en,string hi,string gu)=>ReminderLanguage.Text(language,en,hi,gu)+(en.EndsWith(" ")?" ":"");
        var r=MarketConversation.Normalize(request with {Commodity=request.Commodity.Trim(),State=request.State.Trim(),District=request.District.Trim(),Market=request.Market.Trim()});
        var missing=new List<string>();
        if(string.IsNullOrWhiteSpace(r.Commodity))missing.Add(Local("crop","फसल","પાક"));
        if(string.IsNullOrWhiteSpace(r.District)&&string.IsNullOrWhiteSpace(r.Market))missing.Add(Local("mandi or district","मंडी या जिला","મંડી અથવા જિલ્લો"));
        if(string.IsNullOrWhiteSpace(r.State))missing.Add(Local("state","राज्य","રાજ્ય"));
        var known=string.Join(", ",new[]{r.Commodity,r.Market,r.District,r.State}.Where(v=>!string.IsNullOrWhiteSpace(v)));
        if(missing.Count>0)return (known.Length>0?known+" — ":"")+Local("Please tell me only: ","बस यह जानकारी दें: ","માત્ર આ માહિતી જણાવો: ")+string.Join(", ",missing)+"?";
        var unavailable=known+" — "+Local("Verified mandi prices are temporarily unavailable. Please check the official source; I will not estimate today's price.","अभी सत्यापित मंडी भाव उपलब्ध नहीं हैं। आधिकारिक स्रोत पर जाँचें; मैं आज का भाव अनुमान से नहीं बताऊँगा।","હાલ ચકાસેલા મંડી ભાવ ઉપલબ્ધ નથી. સત્તાવાર સ્ત્રોત તપાસો; હું આજનો ભાવ અંદાજથી નહીં આપું.")+"\n"+Source;
        var key=config["MarketPrices:ApiKey"];
        async Task<string> Fallback(bool noReport=false) {
            if (config.GetValue<bool>("MarketPrices:OfficialSourceEnabled")) {
                var official=await new OfficialMarketPriceService(clients,cache).AnswerAsync(r,language,ct);
                if (official is not null) return official;
            }
            if (noReport) return Local("No verified report found for this crop and location. Check the commodity/mandi spelling or try a nearby mandi. No price has been estimated.","इस फसल और स्थान की सत्यापित report नहीं मिली। फसल/मंडी का नाम जाँचें या पास की मंडी पूछें। कोई अनुमानित भाव नहीं दिया गया है।","આ પાક અને સ્થળનો ચકાસેલો અહેવાલ મળ્યો નથી. પાક/મંડીનું નામ તપાસો અથવા નજીકની મંડી પૂછો. અંદાજિત ભાવ આપ્યો નથી.")+"\n"+Source;
            return string.IsNullOrWhiteSpace(key)?unavailable.Replace(Source,OfficialMarketPriceService.Source,StringComparison.Ordinal):unavailable;
        }
        if (string.IsNullOrWhiteSpace(key) && config.GetValue<bool>("MarketPrices:OfficialSourceEnabled"))
            return await Fallback();
        if(string.IsNullOrWhiteSpace(key)) return known+" — "+Local("The live mandi-price connection is not active on this website yet, so I cannot confirm a numeric price. You do not need to repeat the crop or location. Check the official market report here:","इस वेबसाइट का live मंडी-भाव connection अभी चालू नहीं है, इसलिए सही दाम की पुष्टि नहीं कर सकता। फसल या स्थान दोबारा बताने की जरूरत नहीं है। आधिकारिक मंडी report यहाँ देखें:","આ વેબસાઇટનું live મંડી-ભાવ connection હજી ચાલુ નથી, તેથી ચોક્કસ ભાવની પુષ્ટિ કરી શકતો નથી. પાક કે સ્થળ ફરી જણાવવાની જરૂર નથી. સત્તાવાર મંડી અહેવાલ અહીં જુઓ:")+"\nhttps://www.enam.gov.in/web/dashboard/agmarknet";
        if(new[]{r.Commodity,r.State,r.District,r.Market}.Any(v=>v.Length>100))return unavailable;
        var backoffKey="mandi-backoff:"+Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(key)));
        async Task<string> Failed() {cache.Set(backoffKey,true,TimeSpan.FromSeconds(30));return await Fallback();}
        var cacheKey="mandi:"+JsonSerializer.Serialize(r).ToLowerInvariant();
        if(!cache.TryGetValue(cacheKey,out List<Price>? rows)) {
            if(cache.TryGetValue(backoffKey,out bool coolingDown) && coolingDown)return await Fallback();
            try {
                var url="https://api.data.gov.in/resource/9ef84268-d588-465a-a308-a864a43d0070?format=json&limit=100&api-key="+Uri.EscapeDataString(key);
                foreach(var pair in new[]{("commodity",r.Commodity),("state.keyword",r.State),("district",r.District),("market",r.Market)})
                    if(pair.Item2.Length>0)url+="&"+Uri.EscapeDataString("filters["+pair.Item1+"]")+"="+Uri.EscapeDataString(pair.Item2);
                using var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct);timeout.CancelAfter(TimeSpan.FromSeconds(6));
                using var apiRequest=new HttpRequestMessage(HttpMethod.Get,url);
                apiRequest.Headers.Accept.ParseAdd("application/json");
                apiRequest.Headers.Referrer=new Uri("https://www.data.gov.in/");
                apiRequest.Headers.TryAddWithoutValidation("Origin","https://www.data.gov.in");
                apiRequest.Headers.UserAgent.ParseAdd("DigitalKrushiOfficer/1.0");
                using var response=await clients.CreateClient("market-prices").SendAsync(apiRequest,timeout.Token);
                if(!response.IsSuccessStatusCode) { logger?.LogWarning("Daily mandi API returned HTTP {StatusCode}; using official fallback.",(int)response.StatusCode); return await Failed(); }
                using var doc=JsonDocument.Parse(await response.Content.ReadAsStringAsync(timeout.Token));
                if(!doc.RootElement.TryGetProperty("records",out var records)||records.ValueKind!=JsonValueKind.Array) { logger?.LogWarning("Daily mandi API returned an invalid records payload; using official fallback."); return await Failed(); }
                rows=new();var today=DateOnly.FromDateTime(DateTime.UtcNow.AddHours(5.5));
                foreach(var row in records.EnumerateArray()) {
                    string Get(string name)=>row.TryGetProperty(name,out var value)?value.ToString().Trim():"";
                    bool Matches(string name,string expected)=>expected.Length==0||Get(name).Equals(expected,StringComparison.OrdinalIgnoreCase);
                    if(!Matches("commodity",r.Commodity)||!Matches("state",r.State)||!Matches("district",r.District)||!Matches("market",r.Market))continue;
                    if(!DateOnly.TryParseExact(Get("arrival_date"),new[]{"dd/MM/yyyy","d/M/yyyy","yyyy-MM-dd"},CultureInfo.InvariantCulture,DateTimeStyles.None,out var date)||date>today)continue;
                    bool Amount(string name,out decimal value)=>decimal.TryParse(Get(name),NumberStyles.Number,CultureInfo.InvariantCulture,out value);
                    if(!Amount("min_price",out var min)||!Amount("max_price",out var max)||!Amount("modal_price",out var modal)||min<=0||max<min||modal<min||modal>max)continue;
                    rows.Add(new(Get("commodity"),Get("state"),Get("district"),Get("market"),Get("variety"),date,min,max,modal));
                }
                // Never reuse a failed or empty lookup as a price report.
                logger?.LogInformation("Daily mandi API returned {ReceivedCount} records; {ValidCount} passed location, date and price validation.",records.GetArrayLength(),rows.Count);
                if(rows.Count>0)cache.Set(cacheKey,rows,TimeSpan.FromMinutes(5));
            } catch(Exception ex) when(ex is HttpRequestException or TaskCanceledException or JsonException) {
                // Exception messages may contain the credential-bearing request URL.
                logger?.LogWarning("Daily mandi API failed with {FailureType}, category {Category}, inner type {InnerType}; using official fallback.",
                    ex.GetType().Name, ex is HttpRequestException http ? http.HttpRequestError.ToString() : "NotApplicable", ex.InnerException?.GetType().Name ?? "None");
                return await Failed();
            }
        }
        if(rows==null||rows.Count==0)return await Fallback(noReport:true);
        var latest=rows.Max(p=>p.Date);var stale=latest<DateOnly.FromDateTime(DateTime.UtcNow.AddHours(5.5));
        var title=Local("Reported wholesale mandi prices (INR per quintal; 1 quintal = 100 kg). Modal is the commonly reported trading price, not a guaranteed selling price.","मंडी के रिपोर्ट किए गए थोक भाव (₹/क्विंटल; 1 क्विंटल = 100 किलो)। Modal प्रचलित व्यापार भाव है, बिक्री की गारंटी नहीं।","મંડીના નોંધાયેલા જથ્થાબંધ ભાવ (₹/ક્વિન્ટલ; 1 ક્વિન્ટલ = 100 કિલો). Modal પ્રચલિત વેપાર ભાવ છે, વેચાણની ખાતરી નથી.");
        if(stale)title+="\n"+Local("Older report — these are NOT today's confirmed prices.","पुरानी report — ये आज के पुष्टि किए गए भाव नहीं हैं।","જૂનો અહેવાલ — આ આજના પુષ્ટિ કરેલા ભાવ નથી.");
        var lines=rows.Where(p=>p.Date==latest).OrderBy(p=>p.Market).ThenBy(p=>p.Variety).Take(8).Select(p=>$"{p.Commodity} · {p.Variety}\n{p.Market}, {p.District}, {p.State} · {p.Date.ToString("dd/MM/yyyy",CultureInfo.InvariantCulture)}\n{Local("Min","न्यूनतम","ઓછામાં ઓછો")} ₹{p.Min.ToString("0.##",CultureInfo.InvariantCulture)} | {Local("Modal","प्रचलित भाव","પ્રચલિત ભાવ")} ₹{p.Modal.ToString("0.##",CultureInfo.InvariantCulture)} | {Local("Max","अधिकतम","વધુમાં વધુ")} ₹{p.Max.ToString("0.##",CultureInfo.InvariantCulture)} / {Local("quintal","क्विंटल","ક્વિન્ટલ")}");
        return title+"\n\n"+string.Join("\n\n",lines)+"\n\n"+Local("Source: AGMARKNET / data.gov.in. Reports update when mandis submit data; availability varies. Up to 8 matching entries shown. Cache: up to 5 minutes.","स्रोत: AGMARKNET / data.gov.in. मंडी की reporting पर updates निर्भर हैं। अधिकतम 8 entries दिखाई गई हैं। Cache अधिकतम 5 मिनट।","સ્ત્રોત: AGMARKNET / data.gov.in. અપડેટ મંડીની reporting પર આધારિત છે. વધુમાં વધુ 8 entries બતાવી છે. Cache વધુમાં વધુ 5 મિનિટ.")+"\n"+Source;
    }
}

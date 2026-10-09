using System.Text.Json;
using Digital_Krushi_Officer.Models.ViewModels;
using Microsoft.Extensions.Caching.Memory;
namespace Digital_Krushi_Officer.Services;
public interface IWeatherService { Task<WeatherViewModel?> GetWeatherAsync(string district, string village = ""); }
public class WeatherService(IConfiguration config, ILogger<WeatherService> logger, IHttpClientFactory clients, IMemoryCache cache) : IWeatherService {
    public async Task<WeatherViewModel?> GetWeatherAsync(string district, string village = "") {
        if (string.IsNullOrWhiteSpace(district) || district.Length>100 || village.Length>100) return null;
        var key=config["AISettings:OpenWeatherApiKey"];
        if (string.IsNullOrWhiteSpace(key) || key.StartsWith("your-")) return null;
        var cacheKey="forecast:"+district.ToLowerInvariant();
        if (cache.TryGetValue<WeatherViewModel>(cacheKey,out var cached)) return cached;
        try {
            // District lookup is intentional: arbitrary village names are not provider city IDs.
            var url=$"https://api.openweathermap.org/data/2.5/forecast?q={Uri.EscapeDataString(district)},IN&appid={Uri.EscapeDataString(key)}&units=metric&cnt=1";
            using var response=await clients.CreateClient("providers").GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;
            using var doc=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var item=doc.RootElement.GetProperty("list")[0]; var main=item.GetProperty("main");
            var temp=main.GetProperty("temp").GetDecimal(); var wind=item.GetProperty("wind").GetProperty("speed").GetDecimal()*3.6m;
            var rain=item.GetProperty("pop").GetDecimal()*100m;
            var model=new WeatherViewModel { District=district, Village="", Temperature=Math.Round(temp,1), Humidity=main.GetProperty("humidity").GetDecimal(),
                RainChance=Math.Round(rain), WindSpeed=Math.Round(wind,1), Description=item.GetProperty("weather")[0].GetProperty("description").GetString() ?? "",
                ForecastAt=DateTimeOffset.FromUnixTimeSeconds(item.GetProperty("dt").GetInt64()).UtcDateTime, FetchedAt=DateTime.UtcNow,
                AlertType=rain>=70 ? "Rain" : temp>=40 ? "Heat" : wind>=30 ? "Wind" : "Normal",
                FarmingAlert=rain>=70 ? "Rain is possible during this forecast period. Check field drainage and local conditions." : temp>=40 ? "High temperature forecast. Plan strenuous field work for cooler hours." : wind>=30 ? "Strong winds forecast. Secure loose equipment and check local conditions." : "Check this forecast alongside conditions in your field before planning work." };
            cache.Set(cacheKey,model,TimeSpan.FromMinutes(15)); return model;
        } catch(Exception ex) when(ex is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException or IndexOutOfRangeException) { logger.LogWarning("Weather unavailable: {Type}",ex.GetType().Name); return null; }
    }
}

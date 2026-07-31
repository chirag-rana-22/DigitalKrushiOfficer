using System.Text.Json;
using Digital_Krushi_Officer.Data;
using Digital_Krushi_Officer.Models;
using Digital_Krushi_Officer.Models.ViewModels;

namespace Digital_Krushi_Officer.Services
{
    public interface IWeatherService
    {
        Task<WeatherViewModel?> GetWeatherAsync(string district, string village = "");
    }

    public class WeatherService : IWeatherService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<WeatherService> _logger;
        private readonly HttpClient _http;
        private readonly AppDbContext _db;

        public WeatherService(IConfiguration config, ILogger<WeatherService> logger,
            IHttpClientFactory httpFactory, AppDbContext db)
        {
            _config = config;
            _logger = logger;
            _http = httpFactory.CreateClient();
            _db = db;
        }

        public async Task<WeatherViewModel?> GetWeatherAsync(string district, string village = "")
        {
            try
            {
                var apiKey = _config["AISettings:OpenWeatherApiKey"];
                if (!string.IsNullOrEmpty(apiKey) && apiKey != "your-openweather-api-key")
                {
                    var location = string.IsNullOrEmpty(village) ? district : $"{village},{district}";
                    var url = $"https://api.openweathermap.org/data/2.5/weather?q={Uri.EscapeDataString(location)},IN&appid={apiKey}&units=metric";
                    var resp = await _http.GetAsync(url);
                    if (resp.IsSuccessStatusCode)
                    {
                        var json = await resp.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;
                        var temp = root.GetProperty("main").GetProperty("temp").GetDecimal();
                        var humidity = root.GetProperty("main").GetProperty("humidity").GetDecimal();
                        var desc = root.GetProperty("weather")[0].GetProperty("description").GetString() ?? "";
                        var icon = root.GetProperty("weather")[0].GetProperty("icon").GetString() ?? "";
                        var wind = root.GetProperty("wind").GetProperty("speed").GetDecimal();
                        var rain = root.TryGetProperty("rain", out var rainProp) ? 60m : 15m;

                        var model = BuildWeatherViewModel(district, village, temp, humidity, rain, wind, desc, icon);
                        await LogWeatherAsync(model);
                        return model;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Weather API call failed for {District}", district);
            }

            // Fallback mock data
            return GetMockWeather(district, village);
        }

        private WeatherViewModel BuildWeatherViewModel(string district, string village,
            decimal temp, decimal humidity, decimal rain, decimal wind, string desc, string icon)
        {
            var alert = GenerateFarmingAlert(temp, humidity, rain, wind);
            return new WeatherViewModel
            {
                District = district,
                Village = village,
                Temperature = Math.Round(temp, 1),
                Humidity = Math.Round(humidity, 1),
                RainChance = Math.Round(rain, 1),
                WindSpeed = Math.Round(wind, 1),
                Description = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(desc),
                IconCode = icon,
                FarmingAlert = alert.Message,
                AlertType = alert.Type,
                FetchedAt = DateTime.Now
            };
        }

        private (string Message, string Type) GenerateFarmingAlert(decimal temp, decimal humidity, decimal rain, decimal wind)
        {
            if (rain > 70)
                return ("Heavy rain expected. Delay pesticide spraying. Ensure proper drainage in fields.", "Rain");
            if (temp > 40)
                return ("Extreme heat alert! Irrigate crops early morning or evening. Provide shade to seedlings.", "Heat");
            if (temp < 10)
                return ("Cold wave alert! Protect young crops with covers. Delay sowing of warm-season crops.", "Cold");
            if (wind > 30)
                return ("Strong winds expected. Stake tall plants. Avoid spraying pesticides today.", "Wind");
            if (humidity > 85)
                return ("High humidity may increase fungal disease risk. Monitor crops for blight and mildew.", "Rain");
            return ("Weather is favorable for farming activities. Good time for field operations.", "Normal");
        }

        private async Task LogWeatherAsync(WeatherViewModel model)
        {
            var log = new TblWeatherLog
            {
                District = model.District,
                Village = model.Village,
                Temperature = model.Temperature,
                Humidity = model.Humidity,
                RainChance = model.RainChance,
                WindSpeed = model.WindSpeed,
                WeatherDescription = model.Description,
                FarmingAlert = model.FarmingAlert,
                AlertType = model.AlertType,
                CreatedDate = DateTime.Now
            };
            _db.WeatherLogs.Add(log);
            await _db.SaveChangesAsync();
        }

        private WeatherViewModel GetMockWeather(string district, string village)
        {
            var temp = 32m;
            var humidity = 65m;
            var rain = 20m;
            var wind = 12m;
            return BuildWeatherViewModel(district, village, temp, humidity, rain, wind, "partly cloudy", "02d");
        }
    }
}

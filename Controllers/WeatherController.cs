using Digital_Krushi_Officer.Data;
using Digital_Krushi_Officer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Digital_Krushi_Officer.Controllers
{
    [Authorize]
    public class WeatherController : Controller
    {
        private readonly IWeatherService _weather;
        private readonly AppDbContext _db;

        public WeatherController(IWeatherService weather, AppDbContext db)
        {
            _weather = weather;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var district = User.FindFirstValue("District") ?? "Ahmedabad";
            var village = User.FindFirstValue("Village") ?? "";
            var model = await _weather.GetWeatherAsync(district, village);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Search(string district, string? village)
        {
            var model = await _weather.GetWeatherAsync(district, village ?? "");
            return View("Index", model);
        }
    }
}

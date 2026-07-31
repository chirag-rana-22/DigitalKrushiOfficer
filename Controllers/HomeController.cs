using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Digital_Krushi_Officer.Models;

namespace Digital_Krushi_Officer.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                return role switch
                {
                    "Admin" => RedirectToAction("Index", "Admin"),
                    "Officer" => RedirectToAction("Index", "Officer"),
                    _ => RedirectToAction("Index", "Farmer")
                };
            }
            return View();
        }

        public IActionResult Privacy() => View();

        public IActionResult AccessDenied() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
            => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

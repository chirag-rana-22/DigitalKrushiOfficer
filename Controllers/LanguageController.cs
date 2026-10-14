using System.Security.Claims;
using Digital_Krushi_Officer.Data;
using Digital_Krushi_Officer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Digital_Krushi_Officer.Controllers;
public class LanguageController(AppDbContext db) : Controller
{
    [HttpPost]
    public async Task<IActionResult> Change(string language, string? returnUrl)
    {
        if (!SiteLanguage.Supported(language)) return BadRequest("Unsupported language.");
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await db.Users.FindAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));
            if (user == null) return Unauthorized();
            user.PreferredLanguage = language;
            await db.SaveChangesAsync();
            var authentication = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, AccountSecurity.Principal(user), authentication.Properties);
        }
        Response.Cookies.Append(SiteLanguage.CookieName, language, new CookieOptions { HttpOnly = true, Secure = Request.IsHttps, SameSite = SameSiteMode.Lax, IsEssential = true, MaxAge = TimeSpan.FromDays(365), Path = "/" });
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
    }
}

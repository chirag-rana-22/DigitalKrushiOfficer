using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Digital_Krushi_Officer.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
namespace Digital_Krushi_Officer.Services;
public static class AccountSecurity
{
    public static string Stamp(TblUser u) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(u.PasswordHash + "|" + u.Role)));
    public static ClaimsPrincipal Principal(TblUser u) => new(new ClaimsIdentity(new[] {
        new Claim(ClaimTypes.NameIdentifier, u.Id.ToString()), new Claim(ClaimTypes.Name, u.Name),
        new Claim(ClaimTypes.Email, u.Email), new Claim(ClaimTypes.Role, u.Role),
        new Claim("District", u.District ?? ""), new Claim("Village", u.Village ?? ""),
        new Claim("Language", u.PreferredLanguage), new Claim("SecurityStamp", Stamp(u))
    }, CookieAuthenticationDefaults.AuthenticationScheme));
}

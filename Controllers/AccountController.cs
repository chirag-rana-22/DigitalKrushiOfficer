using Digital_Krushi_Officer.Data;
using Digital_Krushi_Officer.Models;
using Digital_Krushi_Officer.Models.ViewModels;
using Digital_Krushi_Officer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
namespace Digital_Krushi_Officer.Controllers;
[EnableRateLimiting("account")]
public class AccountController(AppDbContext db, OtpService otp, ILogger<AccountController> logger) : Controller
{
    [HttpGet] public IActionResult Register() => View(new RegisterViewModel { PreferredLanguage = SiteLanguage.Current(HttpContext) });
    [HttpPost] public async Task<IActionResult> Register(RegisterViewModel model) {
        if (model.Role is not ("Farmer" or "Officer")) ModelState.AddModelError("Role", "Select Farmer or Officer.");
        if (!ModelState.IsValid) return View(model);
        var address = model.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == address)) { ModelState.AddModelError("", "Unable to register. Try signing in or recovering your account."); return View(model); }
        var user = new TblUser { Name = model.Name.Trim(), Email = address, Mobile = model.Mobile,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password), Role = model.Role,
            State = model.State, District = model.District, Village = model.Village,
            PreferredLanguage = model.PreferredLanguage, IsActive = model.Role == "Farmer", IsEmailVerified = false };
        if (model.Role == "Farmer") user.FarmerDetails = new TblFarmerDetails(); else user.OfficerDetails = new TblOfficerDetails();
        db.Users.Add(user);
        try { await db.SaveChangesAsync(); } catch (DbUpdateException ex) { logger.LogWarning(ex, "Registration could not be saved"); ModelState.AddModelError("", "Unable to register. Please try again."); return View(model); }
        var sent = await otp.SendAsync(user, "verify"); TempData["VerifyEmail"] = address;
        TempData["Warning"] = sent ? "Enter your email code. Officer accounts also need administrator approval." : "Your account is saved, but email delivery is unavailable. Resend the code after a minute.";
        return RedirectToAction(nameof(VerifyOtp));
    }
    [HttpGet] public IActionResult VerifyOtp() => View(new VerifyOtpViewModel { Email = TempData.Peek("VerifyEmail")?.ToString() ?? "" });
    [HttpPost] public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model) {
        if (!ModelState.IsValid) return View(model);
        var address = model.Email.Trim().ToLowerInvariant();
        if (!await otp.VerifyAsync(address, "verify", model.OTPCode)) { ModelState.AddModelError("OTPCode", "Invalid or expired code. After five attempts, request a new code."); return View(model); }
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == address); if (user == null) return BadRequest();
        user.IsEmailVerified = true; await db.SaveChangesAsync();
        TempData["Success"] = user.IsActive ? "Email verified. You can now sign in." : "Email verified. Your officer account awaits administrator approval.";
        return RedirectToAction(nameof(Login));
    }
    [HttpPost] public async Task<IActionResult> ResendOtp(string email) {
        var address = (email ?? "").Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == address && !u.IsEmailVerified);
        if (user != null) await otp.SendAsync(user, "verify");
        TempData["VerifyEmail"] = address; TempData["Warning"] = "If the account needs verification, a code will be sent when email is available. Allow one minute between requests.";
        return RedirectToAction(nameof(VerifyOtp));
    }
    [HttpGet] public IActionResult Login() => View();
    [HttpPost] public async Task<IActionResult> Login(LoginViewModel model) {
        if (!ModelState.IsValid) return View(model);
        var address = model.Email.Trim().ToLowerInvariant(); var user = await db.Users.SingleOrDefaultAsync(u => u.Email == address);
        bool valid = false;
        try { valid = user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash); } catch (BCrypt.Net.SaltParseException) { }
        if (!valid || user == null) { ModelState.AddModelError("", "Invalid email or password."); return View(model); }
        if (!user.IsEmailVerified) { TempData["VerifyEmail"] = user.Email; return RedirectToAction(nameof(VerifyOtp)); }
        if (!user.IsActive) { ModelState.AddModelError("", "Your account awaits approval or is disabled. Contact your administrator."); return View(model); }
        user.LastLoginDate = DateTime.Now; await db.SaveChangesAsync();
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, AccountSecurity.Principal(user), new AuthenticationProperties { IsPersistent = model.RememberMe });
        return RedirectToAction("Index", user.Role is "Admin" or "Officer" ? user.Role : "Farmer");
    }
    [HttpPost] public async Task<IActionResult> Logout() { await HttpContext.SignOutAsync(); HttpContext.Session.Clear(); return RedirectToAction("Index", "Home"); }
    [HttpGet] public IActionResult ForgotPassword() => View();
    [HttpPost] public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model) {
        if (!ModelState.IsValid) return View(model);
        var address = model.Email.Trim().ToLowerInvariant(); var user = await db.Users.SingleOrDefaultAsync(u => u.Email == address && u.IsActive && u.IsEmailVerified);
        if (user != null) await otp.SendAsync(user, "reset");
        TempData["ResetEmail"] = address; TempData["Warning"] = "If an eligible account exists, a reset code will be sent. Allow a minute before requesting another.";
        return RedirectToAction(nameof(ResetPassword));
    }
    [HttpGet] public IActionResult ResetPassword() => View(new ResetPasswordViewModel { Email = TempData.Peek("ResetEmail")?.ToString() ?? "" });
    [HttpPost] public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model) {
        if (!ModelState.IsValid) return View(model);
        var address = model.Email.Trim().ToLowerInvariant();
        if (!await otp.VerifyAsync(address, "reset", model.Token)) { ModelState.AddModelError("Token", "Invalid or expired code. Request a new code after five attempts."); return View(model); }
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == address && u.IsActive); if (user == null) return BadRequest();
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword); user.UpdatedDate = DateTime.Now; await db.SaveChangesAsync();
        TempData["Success"] = "Password changed. Sign in again on your devices."; return RedirectToAction(nameof(Login));
    }

    [Microsoft.AspNetCore.Authorization.Authorize, HttpGet]
    public IActionResult ChangePassword() => View();
    [Microsoft.AspNetCore.Authorization.Authorize, HttpPost]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model) {
        if (!ModelState.IsValid) return View(model);
        var id = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var user = await db.Users.FindAsync(id);
        if (user == null || !BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash)) {
            ModelState.AddModelError("CurrentPassword", "Current password is incorrect."); return View(model);
        }
        if (model.CurrentPassword == model.NewPassword) { ModelState.AddModelError("NewPassword", "Choose a different password."); return View(model); }
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword); user.UpdatedDate = DateTime.Now;
        await db.SaveChangesAsync(); await HttpContext.SignOutAsync();
        TempData["Success"] = "Password changed. Sign in with your new password."; return RedirectToAction(nameof(Login));
    }
}

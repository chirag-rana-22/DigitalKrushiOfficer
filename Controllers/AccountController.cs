using Digital_Krushi_Officer.Data;
using Digital_Krushi_Officer.Models;
using Digital_Krushi_Officer.Models.ViewModels;
using Digital_Krushi_Officer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Digital_Krushi_Officer.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IEmailService _email;

        public AccountController(AppDbContext db, IEmailService email)
        {
            _db = db;
            _email = email;
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Validate role is set
            if (string.IsNullOrEmpty(model.Role))
            {
                ModelState.AddModelError("Role", "Please select a role.");
                return View(model);
            }

            if (await _db.Users.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email is already registered.");
                return View(model);
            }

            TblUser? user = null;
            try
            {
                var now = DateTime.Now;
                user = new TblUser
                {
                    Name = model.Name,
                    Email = model.Email,
                    Mobile = model.Mobile,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    Role = model.Role,
                    State = model.State,
                    District = model.District,
                    Village = model.Village,
                    PreferredLanguage = model.PreferredLanguage,
                    IsEmailVerified = false,
                    IsActive = true,
                    CreatedDate = now
                };
                _db.Users.Add(user);
                await _db.SaveChangesAsync();

                // Create role-specific details
                if (model.Role == "Farmer")
                {
                    var farmerDetails = new TblFarmerDetails
                    {
                        UserId = user.Id,
                        IsActive = true,
                        CreatedDate = now
                    };
                    _db.FarmerDetails.Add(farmerDetails);
                }
                else if (model.Role == "Officer")
                {
                    var officerDetails = new TblOfficerDetails
                    {
                        UserId = user.Id,
                        IsActive = true,
                        CreatedDate = now
                    };
                    _db.OfficerDetails.Add(officerDetails);
                }
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Registration failed: {ex.InnerException?.Message ?? ex.Message}");
                return View(model);
            }

            if (user == null)
            {
                ModelState.AddModelError("", "Failed to create user account.");
                return View(model);
            }

            // Generate OTP and store in Session (not DB)
            var otp = GenerateOtp();
            HttpContext.Session.SetString("OTP_" + user.Email, otp);
            HttpContext.Session.SetString("OTP_Expiry_" + user.Email, DateTime.Now.AddMinutes(10).ToString("o"));

            // Send OTP email
            var emailError = await _email.SendOtpEmailAsync(user.Email, user.Name, otp);
            if (emailError != null)
            {
                // Email failed — still allow user to proceed but show warning
                TempData["Warning"] = $"Could not send OTP email: {emailError}. Check spam or contact support.";
            }

            // Also send welcome email (fire-and-forget)
            _ = _email.SendWelcomeEmailAsync(user.Email, user.Name, user.Role);

            TempData["VerifyEmail"] = user.Email;
            TempData["Success"] = "Registration successful! Please verify your email.";
            return RedirectToAction("VerifyOtp");
        }

        [HttpGet]
        public IActionResult VerifyOtp()
        {
            var email = TempData["VerifyEmail"]?.ToString() ?? "";
            TempData.Keep("VerifyEmail");
            return View(new VerifyOtpViewModel { Email = email });
        }

        [HttpPost]
        public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null) { ModelState.AddModelError("", "User not found."); return View(model); }

            // Retrieve OTP from Session
            var sessionOtp    = HttpContext.Session.GetString("OTP_" + model.Email);
            var sessionExpiry = HttpContext.Session.GetString("OTP_Expiry_" + model.Email);

            if (string.IsNullOrEmpty(sessionOtp) || string.IsNullOrEmpty(sessionExpiry))
            {
                ModelState.AddModelError("OTPCode", "OTP session expired. Please register again or request a new OTP.");
                return View(model);
            }

            if (!DateTime.TryParse(sessionExpiry, out var expiry) || DateTime.Now > expiry)
            {
                HttpContext.Session.Remove("OTP_" + model.Email);
                HttpContext.Session.Remove("OTP_Expiry_" + model.Email);
                ModelState.AddModelError("OTPCode", "OTP has expired. Please request a new one.");
                return View(model);
            }

            if (sessionOtp != model.OTPCode.Trim())
            {
                ModelState.AddModelError("OTPCode", "Invalid OTP. Please try again.");
                return View(model);
            }

            // OTP valid — mark email as verified and clear session keys
            user.IsEmailVerified = true;
            await _db.SaveChangesAsync();

            HttpContext.Session.Remove("OTP_" + model.Email);
            HttpContext.Session.Remove("OTP_Expiry_" + model.Email);

            TempData["Success"] = "Email verified! Please login.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == model.Email && u.IsActive);
            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            if (!user.IsEmailVerified)
            {
                TempData["VerifyEmail"] = user.Email;
                TempData["Warning"] = "Please verify your email first.";
                return RedirectToAction("VerifyOtp");
            }

            user.LastLoginDate = DateTime.Now;
            await _db.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Name),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role),
                new("District", user.District ?? ""),
                new("Village", user.Village ?? ""),
                new("Language", user.PreferredLanguage)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var props = new AuthenticationProperties { IsPersistent = model.RememberMe };
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), props);

            return user.Role switch
            {
                "Admin" => RedirectToAction("Index", "Admin"),
                "Officer" => RedirectToAction("Index", "Officer"),
                _ => RedirectToAction("Index", "Farmer")
            };
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == model.Email && u.IsActive);
            if (user == null) { TempData["Error"] = "Email not found."; return View(model); }

            // Generate OTP and store in Session
            var otp = GenerateOtp();
            HttpContext.Session.SetString("RESET_OTP_" + user.Email, otp);
            HttpContext.Session.SetString("RESET_OTP_Expiry_" + user.Email, DateTime.Now.AddMinutes(10).ToString("o"));

            var emailError = await _email.SendPasswordResetEmailAsync(user.Email, user.Name, otp);
            if (emailError != null)
            {
                TempData["Error"] = $"Could not send reset email: {emailError}";
                return View(model);
            }

            TempData["ResetEmail"] = user.Email;
            return RedirectToAction("ResetPassword");
        }

        [HttpGet]
        public IActionResult ResetPassword()
        {
            var email = TempData["ResetEmail"]?.ToString() ?? "";
            TempData.Keep("ResetEmail");
            return View(new ResetPasswordViewModel { Email = email });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null) { ModelState.AddModelError("", "User not found."); return View(model); }

            // Verify OTP from Session
            var sessionOtp    = HttpContext.Session.GetString("RESET_OTP_" + model.Email);
            var sessionExpiry = HttpContext.Session.GetString("RESET_OTP_Expiry_" + model.Email);

            if (string.IsNullOrEmpty(sessionOtp))
            {
                ModelState.AddModelError("Token", "OTP session expired. Please request a new reset link.");
                return View(model);
            }

            if (!DateTime.TryParse(sessionExpiry, out var expiry) || DateTime.Now > expiry)
            {
                HttpContext.Session.Remove("RESET_OTP_" + model.Email);
                HttpContext.Session.Remove("RESET_OTP_Expiry_" + model.Email);
                ModelState.AddModelError("Token", "OTP has expired. Please request a new one.");
                return View(model);
            }

            if (sessionOtp != model.Token.Trim())
            {
                ModelState.AddModelError("Token", "Invalid OTP. Please try again.");
                return View(model);
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            user.UpdatedDate = DateTime.Now;
            await _db.SaveChangesAsync();

            HttpContext.Session.Remove("RESET_OTP_" + model.Email);
            HttpContext.Session.Remove("RESET_OTP_Expiry_" + model.Email);

            TempData["Success"] = "Password reset successfully!";
            return RedirectToAction("Login");
        }

        private static string GenerateOtp() => new Random().Next(100000, 999999).ToString();
    }
}

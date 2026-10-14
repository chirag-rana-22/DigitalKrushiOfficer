using System.ComponentModel.DataAnnotations;

namespace Digital_Krushi_Officer.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required, MaxLength(100)]
        [Display(Name = "Full Name")]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(100)]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(15)]
        [Display(Name = "Mobile Number")]
        public string Mobile { get; set; } = string.Empty;

        [Required, StringLength(64, MinimumLength = 10)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(Farmer|Officer)$")]
        [Display(Name = "Role")]
        public string Role { get; set; } = "Farmer";

        [MaxLength(100)]
        [Display(Name = "State")]
        public string? State { get; set; }

        [MaxLength(100)]
        [Display(Name = "District")]
        public string? District { get; set; }

        [MaxLength(100)]
        [Display(Name = "Village")]
        public string? Village { get; set; }

        [RegularExpression("^(Gujarati|Hindi|English|Marathi)$")]
        [Display(Name = "Preferred Language")]
        public string PreferredLanguage { get; set; } = "English";
    }

    public class LoginViewModel
    {
        [Required, EmailAddress, MaxLength(100)]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember Me")]
        public bool RememberMe { get; set; }
    }

    public class ForgotPasswordViewModel
    {
        [Required, EmailAddress, MaxLength(100)]
        public string Email { get; set; } = string.Empty;
    }

    public class VerifyOtpViewModel
    {
        [Required, EmailAddress, MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required, RegularExpression(@"^\d{6}$", ErrorMessage = "Enter the six-digit code.")]
        [Display(Name = "OTP Code")]
        public string OTPCode { get; set; } = string.Empty;
    }

    public class ResetPasswordViewModel
    {
        [Required, EmailAddress, MaxLength(100)]
        public string Email { get; set; } = string.Empty;
        [Required, RegularExpression(@"^\d{6}$")]
        public string Token { get; set; } = string.Empty;

        [Required, StringLength(64, MinimumLength = 10)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ChangePasswordViewModel {
        [Required, DataType(DataType.Password)] public string CurrentPassword { get; set; } = "";
        [Required, StringLength(64, MinimumLength = 10), DataType(DataType.Password)] public string NewPassword { get; set; } = "";
        [Required, Compare(nameof(NewPassword)), DataType(DataType.Password)] public string ConfirmPassword { get; set; } = "";
    }
}

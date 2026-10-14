using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Digital_Krushi_Officer.Services
{
    public interface IEmailService
    {
        Task<string?> SendEmailAsync(string toEmail, string toName, string subject, string htmlBody);
        Task<string?> SendOtpEmailAsync(string toEmail, string toName, string otpCode, string language = "English");
        Task SendWelcomeEmailAsync(string toEmail, string toName, string role);
        Task<string?> SendPasswordResetEmailAsync(string toEmail, string toName, string resetToken, string language = "English");
        Task SendEscalationNotificationAsync(string toEmail, string officerName, string farmerName, string queryText);
        Task SendOfficerReplyNotificationAsync(string toEmail, string farmerName, string officerName, string reply, string language = "English");
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _clients;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger, IHttpClientFactory clients)
        {
            _config = config;
            _clients = clients;
            _logger = logger;
        }

        public async Task<string?> SendEmailAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            try
            {
                var provider = _config["Email:Provider"] ?? "Smtp";
                if (provider.Equals("Brevo", StringComparison.OrdinalIgnoreCase)) {
                    var key = _config["Email:Brevo:ApiKey"];
                    var sender = _config["Email:FromEmail"];
                    if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(sender)) return "Email delivery is not configured.";
                    using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
                    request.Headers.Add("api-key", key);
                    request.Content = JsonContent.Create(new {
                        sender = new { email = sender, name = _config["Email:FromName"] ?? "Digital Krushi Officer" },
                        to = new[] { new { email = toEmail, name = toName } }, subject, htmlContent = htmlBody
                    });
                    using var response = await _clients.CreateClient("email-api").SendAsync(request);
                    if (response.IsSuccessStatusCode) return null;
                    _logger.LogWarning("Email API rejected delivery with status {Status}", (int)response.StatusCode);
                    return "Email delivery is temporarily unavailable. Please try again later.";
                }
                if (!provider.Equals("Smtp", StringComparison.OrdinalIgnoreCase)) return "Email delivery is not configured.";
                var s        = _config.GetSection("SmtpSettings");
                var host     = s["Host"]     ?? "smtp.gmail.com";
                var portStr  = s["Port"]     ?? "587";
                var username = s["Username"] ?? "";
                var password = s["Password"] ?? "";
                var fromName = s["FromName"] ?? "Digital Krushi Officer";

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    var cfgErr = "SMTP Username or Password is missing in appsettings.json SmtpSettings section.";
                    _logger.LogError("❌ {Error}", cfgErr);
                    return cfgErr;
                }

                if (!int.TryParse(portStr, out var port)) port = 587;

                _logger.LogInformation("📧 Sending email | To:{Email} | Host:{Host}:{Port} | From:{User}", toEmail, host, port, username);

                var msg = new MimeMessage();
                msg.From.Add(new MailboxAddress(fromName, username));
                msg.To.Add(new MailboxAddress(toName, toEmail));
                msg.Subject = subject;
                msg.Body    = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

                using var client = new SmtpClient();
                client.Timeout = 15000;

                _logger.LogInformation("📧 Step 1: Connecting to {Host}:{Port}...", host, port);
                await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);

                _logger.LogInformation("📧 Step 2: Authenticating as {User}...", username);
                await client.AuthenticateAsync(username, password);

                _logger.LogInformation("📧 Step 3: Sending message...");
                await client.SendAsync(msg);
                await client.DisconnectAsync(true);

                _logger.LogInformation("✅ Email sent successfully to {Email}", toEmail);
                return null; // null = success
            }
            catch (Exception ex)
            {
                var inner  = ex.InnerException?.Message ?? "";
                var errMsg = string.IsNullOrEmpty(inner) ? ex.Message : $"{ex.Message} — Inner: {inner}";
                _logger.LogError(ex, "❌ Email FAILED to {Email} | Error: {Error}", toEmail, errMsg);
                return "Email delivery is temporarily unavailable. Please try again later.";
            }
        }


        public async Task<string?> SendOtpEmailAsync(string toEmail, string toName, string otpCode, string language = "English")
        {
            return await SendEmailAsync(toEmail, toName, TranslationCatalog.Translate("OTP Verification", language) + " - Digital Krushi Officer", CodeEmail(toName, otpCode, "OTP Verification", language));
        }
        private static string CodeEmail(string name, string code, string heading, string language) {
            string E(string value) => System.Net.WebUtility.HtmlEncode(value);
            string T(string value) => E(TranslationCatalog.Translate(value, language));
            return $"<html lang='{SiteLanguage.Code(language)}'><body style='font-family:system-ui,sans-serif;max-width:600px;margin:auto;padding:24px'><h1>Digital Krushi Officer</h1><h2>{T(heading)}</h2><p>{E(name)}, {T("Your code is:")}</p><p style='font-size:30px;font-weight:bold;letter-spacing:5px'>{E(code)}</p><p>{T("Valid for 10 minutes. Do not share this code with anyone.")}</p></body></html>";
        }

        public async Task SendWelcomeEmailAsync(string toEmail, string toName, string role)
        {
            var html = $@"<div style='font-family:Poppins,sans-serif;max-width:600px;margin:auto;padding:30px;background:#f8f9fa;border-radius:12px;'>
              <div style='background:linear-gradient(135deg,#2E7D32,#43A047);padding:20px;border-radius:10px;text-align:center;'>
                <h1 style='color:white;margin:0;'>🌾 Digital Krushi Officer</h1></div>
              <div style='background:white;padding:30px;border-radius:10px;margin-top:20px;'>
                <h2 style='color:#2E7D32;'>Welcome, {toName}! 🎉</h2>
                <p>You have successfully registered as a <strong>{role}</strong>.</p>
                <p>Access AI farming advice, weather alerts, and government schemes 24/7!</p></div></div>";
            await SendEmailAsync(toEmail, toName, "Welcome to Digital Krushi Officer!", html);
        }

        public async Task<string?> SendPasswordResetEmailAsync(string toEmail, string toName, string resetToken, string language = "English")
        {
            return await SendEmailAsync(toEmail, toName, TranslationCatalog.Translate("Password Reset", language) + " - Digital Krushi Officer", CodeEmail(toName, resetToken, "Password Reset", language));
        }

        public async Task SendEscalationNotificationAsync(string toEmail, string officerName, string farmerName, string queryText)
        {
            var html = $@"<div style='font-family:Poppins,sans-serif;max-width:600px;margin:auto;padding:30px;background:#f8f9fa;border-radius:12px;'>
              <div style='background:linear-gradient(135deg,#2E7D32,#43A047);padding:20px;border-radius:10px;text-align:center;'>
                <h1 style='color:white;margin:0;'>🌾 Digital Krushi Officer</h1></div>
              <div style='background:white;padding:30px;border-radius:10px;margin-top:20px;'>
                <h2 style='color:#FBC02D;'>⚠ New Escalated Query</h2>
                <p>Dear Officer {officerName}, a query from <strong>{farmerName}</strong> has been escalated to you.</p>
                <div style='background:#FFF8E1;border-left:4px solid #FBC02D;padding:15px;border-radius:5px;'>
                  <p style='margin:0;'><strong>Query:</strong> {queryText}</p></div>
                <p style='color:#757575;'>Please log in to respond.</p></div></div>";
            await SendEmailAsync(toEmail, officerName, "New Escalated Query - Digital Krushi Officer", html);
        }

        public async Task SendOfficerReplyNotificationAsync(string toEmail, string farmerName, string officerName, string reply, string language = "English")
        {
            string E(string value) => System.Net.WebUtility.HtmlEncode(value);
            var heading = TranslationCatalog.Translate("New officer reply", language);
            var message = TranslationCatalog.Translate("Your Krushi Officer has replied. Open your query history to read the full conversation.", language);
            var html = $"<html lang='{SiteLanguage.Code(language)}'><body><h2>{E(heading)}</h2><p>{E(farmerName)}, {E(message)}</p><p>{E(officerName)}</p><blockquote>{E(reply)}</blockquote></body></html>";
            await SendEmailAsync(toEmail, farmerName, heading + " - Digital Krushi Officer", html);
        }
    }
}

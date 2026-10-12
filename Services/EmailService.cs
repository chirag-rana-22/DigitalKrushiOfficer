using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Digital_Krushi_Officer.Services
{
    public interface IEmailService
    {
        Task<string?> SendEmailAsync(string toEmail, string toName, string subject, string htmlBody);
        Task<string?> SendOtpEmailAsync(string toEmail, string toName, string otpCode);
        Task SendWelcomeEmailAsync(string toEmail, string toName, string role);
        Task<string?> SendPasswordResetEmailAsync(string toEmail, string toName, string resetToken);
        Task SendEscalationNotificationAsync(string toEmail, string officerName, string farmerName, string queryText);
        Task SendOfficerReplyNotificationAsync(string toEmail, string farmerName, string officerName, string reply);
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


        public async Task<string?> SendOtpEmailAsync(string toEmail, string toName, string otpCode)
        {
            var html = $@"<div style='font-family:Poppins,sans-serif;max-width:600px;margin:auto;padding:30px;background:#f8f9fa;border-radius:12px;'>
              <div style='background:linear-gradient(135deg,#2E7D32,#43A047);padding:20px;border-radius:10px;text-align:center;'>
                <h1 style='color:white;margin:0;'>🌾 Digital Krushi Officer</h1></div>
              <div style='background:white;padding:30px;border-radius:10px;margin-top:20px;'>
                <h2 style='color:#2E7D32;'>OTP Verification</h2>
                <p>Dear {toName}, your OTP is:</p>
                <div style='background:#E8F5E9;border:2px dashed #2E7D32;border-radius:10px;padding:20px;text-align:center;'>
                  <span style='font-size:36px;font-weight:bold;color:#2E7D32;letter-spacing:10px;'>{otpCode}</span></div>
                <p style='color:#757575;'>Valid for <strong>10 minutes</strong>. Do not share with anyone.</p></div></div>";
            return await SendEmailAsync(toEmail, toName, "OTP Verification - Digital Krushi Officer", html);
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

        public async Task<string?> SendPasswordResetEmailAsync(string toEmail, string toName, string resetToken)
        {
            var html = $@"<div style='font-family:Poppins,sans-serif;max-width:600px;margin:auto;padding:30px;background:#f8f9fa;border-radius:12px;'>
              <div style='background:linear-gradient(135deg,#2E7D32,#43A047);padding:20px;border-radius:10px;text-align:center;'>
                <h1 style='color:white;margin:0;'>🌾 Digital Krushi Officer</h1></div>
              <div style='background:white;padding:30px;border-radius:10px;margin-top:20px;'>
                <h2 style='color:#E53935;'>Password Reset</h2>
                <p>Dear {toName}, your reset OTP is:</p>
                <div style='background:#FFEBEE;border:2px dashed #E53935;padding:20px;text-align:center;border-radius:10px;'>
                  <span style='font-size:36px;font-weight:bold;color:#E53935;letter-spacing:10px;'>{resetToken}</span></div>
                <p style='color:#757575;'>Valid for 10 minutes.</p></div></div>";
            return await SendEmailAsync(toEmail, toName, "Password Reset - Digital Krushi Officer", html);
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

        public async Task SendOfficerReplyNotificationAsync(string toEmail, string farmerName, string officerName, string reply)
        {
            var html = $@"<div style='font-family:Poppins,sans-serif;max-width:600px;margin:auto;padding:30px;background:#f8f9fa;border-radius:12px;'>
              <div style='background:linear-gradient(135deg,#2E7D32,#43A047);padding:20px;border-radius:10px;text-align:center;'>
                <h1 style='color:white;margin:0;'>🌾 Digital Krushi Officer</h1></div>
              <div style='background:white;padding:30px;border-radius:10px;margin-top:20px;'>
                <h2 style='color:#2E7D32;'>✅ Officer Reply Received</h2>
                <p>Dear {farmerName}, Officer <strong>{officerName}</strong> replied:</p>
                <div style='background:#E8F5E9;border-left:4px solid #2E7D32;padding:15px;border-radius:5px;'>
                  <p style='margin:0;'>{reply}</p></div></div></div>";
            await SendEmailAsync(toEmail, farmerName, "Officer Reply - Digital Krushi Officer", html);
        }
    }
}

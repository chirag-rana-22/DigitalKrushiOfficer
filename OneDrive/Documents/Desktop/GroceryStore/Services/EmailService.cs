using System.Net;
using System.Net.Mail;
using System.Text;

namespace GroceryStore.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendOrderConfirmationEmailAsync(string userEmail, string userName, string orderNumber, decimal totalAmount, List<(string productName, int quantity, decimal price)> items)
        {
            try
            {
                var itemsList = string.Join("\n", items.Select(i => $"• {i.productName} x{i.quantity} - ₹{i.price:F2}"));

                var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #28a745; color: white; padding: 20px; text-align: center; border-radius: 5px; }}
        .content {{ margin: 20px 0; }}
        .items {{ background-color: #f5f5f5; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .footer {{ color: #666; font-size: 12px; margin-top: 20px; border-top: 1px solid #ddd; padding-top: 10px; }}
        .total {{ font-size: 18px; font-weight: bold; color: #28a745; margin-top: 15px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h2>Order Confirmed! 🎉</h2>
        </div>
        <div class=""content"">
            <p>Hi {userName},</p>
            <p>Thank you for your order! Your order has been successfully placed and confirmed.</p>
            
            <h3>Order Details:</h3>
            <p><strong>Order Number:</strong> {orderNumber}</p>
            <p><strong>Order Date:</strong> {DateTime.Now:MMMM dd, yyyy}</p>
            
            <h3>Items Ordered:</h3>
            <div class=""items"">
                <pre>{itemsList}</pre>
            </div>
            
            <div class=""total"">
                <p>Total Amount: ₹{totalAmount:F2}</p>
            </div>
            
            <p>You can track your order status in your account dashboard.</p>
            <p>Thank you for shopping with us!</p>
        </div>
        <div class=""footer"">
            <p>This is an automated email. Please do not reply to this email.</p>
            <p>&copy; 2026 Grocery Store. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

                return await SendEmailAsync(userEmail, $"Order Confirmation - {orderNumber}", body);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending order confirmation email to {userEmail}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendPaymentConfirmationEmailAsync(string userEmail, string userName, string orderNumber, string paymentMethod, decimal amount)
        {
            try
            {
                var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #28a745; color: white; padding: 20px; text-align: center; border-radius: 5px; }}
        .content {{ margin: 20px 0; }}
        .success-box {{ background-color: #d4edda; border: 1px solid #c3e6cb; color: #155724; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .footer {{ color: #666; font-size: 12px; margin-top: 20px; border-top: 1px solid #ddd; padding-top: 10px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h2>Payment Successful! ✓</h2>
        </div>
        <div class=""content"">
            <p>Hi {userName},</p>
            
            <div class=""success-box"">
                <h3>Your payment has been processed successfully!</h3>
            </div>
            
            <h3>Payment Details:</h3>
            <ul>
                <li><strong>Order Number:</strong> {orderNumber}</li>
                <li><strong>Payment Method:</strong> {paymentMethod}</li>
                <li><strong>Amount Paid:</strong> ₹{amount:F2}</li>
                <li><strong>Payment Date:</strong> {DateTime.Now:MMMM dd, yyyy hh:mm tt}</li>
            </ul>
            
            <p>Your order is now being prepared for delivery. You'll receive another email once your order is dispatched.</p>
            <p>Thank you for your payment!</p>
        </div>
        <div class=""footer"">
            <p>This is an automated email. Please do not reply to this email.</p>
            <p>&copy; 2026 Grocery Store. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

                return await SendEmailAsync(userEmail, $"Payment Confirmation - {orderNumber}", body);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending payment confirmation email to {userEmail}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendPaymentFailureEmailAsync(string userEmail, string userName, string orderNumber, string errorMessage)
        {
            try
            {
                var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; border-radius: 5px; }}
        .content {{ margin: 20px 0; }}
        .error-box {{ background-color: #f8d7da; border: 1px solid #f5c6cb; color: #721c24; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .footer {{ color: #666; font-size: 12px; margin-top: 20px; border-top: 1px solid #ddd; padding-top: 10px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h2>Payment Failed</h2>
        </div>
        <div class=""content"">
            <p>Hi {userName},</p>
            
            <div class=""error-box"">
                <h3>Your payment could not be processed</h3>
                <p><strong>Error:</strong> {errorMessage}</p>
            </div>
            
            <h3>Order Details:</h3>
            <p><strong>Order Number:</strong> {orderNumber}</p>
            
            <p>Your order has been placed for <strong>Cash on Delivery</strong> instead.</p>
            <p>You can try making an online payment again from your account dashboard, or you can pay the delivery person when your order arrives.</p>
            <p>For assistance, please contact our support team.</p>
        </div>
        <div class=""footer"">
            <p>This is an automated email. Please do not reply to this email.</p>
            <p>&copy; 2026 Grocery Store. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

                return await SendEmailAsync(userEmail, $"Payment Failed - {orderNumber}", body);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending payment failure email to {userEmail}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendOrderStatusEmailAsync(string userEmail, string userName, string orderNumber, string status)
        {
            try
            {
                string statusMessage = status switch
                {
                    "Pending" => "Your order is being prepared for delivery.",
                    "Packed" => "Your order has been packed and is ready for delivery!",
                    "Shipped" => "Your order is on its way!",
                    "Delivered" => "Your order has been delivered. Thank you for shopping!",
                    "Cancelled" => "Your order has been cancelled.",
                    _ => "Your order status has been updated."
                };

                var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; border-radius: 5px; }}
        .content {{ margin: 20px 0; }}
        .status-box {{ background-color: #e7f3ff; border-left: 4px solid #007bff; padding: 15px; margin: 20px 0; }}
        .footer {{ color: #666; font-size: 12px; margin-top: 20px; border-top: 1px solid #ddd; padding-top: 10px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h2>Order Status Update 📦</h2>
        </div>
        <div class=""content"">
            <p>Hi {userName},</p>
            
            <div class=""status-box"">
                <h3>Order Number: {orderNumber}</h3>
                <p><strong>Current Status:</strong> {status}</p>
                <p>{statusMessage}</p>
            </div>
            
            <p>You can track your order in detail by visiting your account dashboard.</p>
            <p>Thank you for your patience!</p>
        </div>
        <div class=""footer"">
            <p>This is an automated email. Please do not reply to this email.</p>
            <p>&copy; 2026 Grocery Store. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

                return await SendEmailAsync(userEmail, $"Order Status Update - {orderNumber}", body);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending order status email to {userEmail}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendDeliveryUpdateEmailAsync(string userEmail, string userName, string orderNumber, string deliveryStatus, string? deliveryDate = null)
        {
            try
            {
                var dateInfo = string.IsNullOrEmpty(deliveryDate) ? "Soon" : deliveryDate;

                var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #17a2b8; color: white; padding: 20px; text-align: center; border-radius: 5px; }}
        .content {{ margin: 20px 0; }}
        .delivery-box {{ background-color: #d1ecf1; border-left: 4px solid #17a2b8; padding: 15px; margin: 20px 0; }}
        .footer {{ color: #666; font-size: 12px; margin-top: 20px; border-top: 1px solid #ddd; padding-top: 10px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h2>Delivery Update 🚚</h2>
        </div>
        <div class=""content"">
            <p>Hi {userName},</p>
            
            <div class=""delivery-box"">
                <h3>Order Number: {orderNumber}</h3>
                <p><strong>Delivery Status:</strong> {deliveryStatus}</p>
                <p><strong>Expected Delivery:</strong> {dateInfo}</p>
            </div>
            
            <p>Your order is being processed and will be delivered soon. Please ensure someone is available to receive the order.</p>
            <p>If you have any questions, please don't hesitate to contact our support team.</p>
        </div>
        <div class=""footer"">
            <p>This is an automated email. Please do not reply to this email.</p>
            <p>&copy; 2026 Grocery Store. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

                return await SendEmailAsync(userEmail, $"Delivery Update - {orderNumber}", body);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending delivery update email to {userEmail}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendWelcomeEmailAsync(string userEmail, string userName)
        {
            try
            {
                var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #28a745; color: white; padding: 20px; text-align: center; border-radius: 5px; }}
        .content {{ margin: 20px 0; }}
        .features {{ background-color: #f5f5f5; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .footer {{ color: #666; font-size: 12px; margin-top: 20px; border-top: 1px solid #ddd; padding-top: 10px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h2>Welcome to Grocery Store! 🎉</h2>
        </div>
        <div class=""content"">
            <p>Hi {userName},</p>
            <p>Thank you for creating an account with us! We're excited to have you as part of our growing community.</p>
            
            <div class=""features"">
                <h3>What you can do now:</h3>
                <ul>
                    <li>Browse and order fresh groceries</li>
                    <li>Track your orders in real-time</li>
                    <li>Save multiple delivery addresses</li>
                    <li>View your order history</li>
                    <li>Receive exclusive offers and updates</li>
                </ul>
            </div>
            
            <p>Start shopping now and enjoy fast delivery to your doorstep!</p>
            <p>If you have any questions or need assistance, our support team is always ready to help.</p>
            <p>Happy shopping!</p>
        </div>
        <div class=""footer"">
            <p>This is an automated email. Please do not reply to this email.</p>
            <p>&copy; 2026 Grocery Store. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

                return await SendEmailAsync(userEmail, "Welcome to Grocery Store!", body);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending welcome email to {userEmail}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendContactFormEmailAsync(string senderName, string senderEmail, string subject, string message)
        {
            try
            {
                var adminEmail = _configuration["Email:AdminEmail"] ?? "admin@grocerystore.com";

                var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; border-radius: 5px; }}
        .content {{ margin: 20px 0; }}
        .message-box {{ background-color: #f5f5f5; border-left: 4px solid #007bff; padding: 15px; margin: 20px 0; white-space: pre-wrap; }}
        .footer {{ color: #666; font-size: 12px; margin-top: 20px; border-top: 1px solid #ddd; padding-top: 10px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h2>New Contact Form Submission</h2>
        </div>
        <div class=""content"">
            <h3>Contact Details:</h3>
            <p><strong>Name:</strong> {senderName}</p>
            <p><strong>Email:</strong> {senderEmail}</p>
            <p><strong>Subject:</strong> {subject}</p>
            
            <h3>Message:</h3>
            <div class=""message-box"">{message}</div>
            
            <p>Please reply to this inquiry at your earliest convenience.</p>
        </div>
        <div class=""footer"">
            <p>&copy; 2026 Grocery Store. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

                return await SendEmailAsync(adminEmail, $"New Contact Form: {subject}", body, senderEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending contact form email: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendPasswordResetOTPAsync(string userEmail, string userName, string otp)
        {
            try
            {
                var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; border-radius: 5px; }}
        .content {{ margin: 20px 0; }}
        .otp-box {{ background-color: #f8d7da; border-left: 4px solid #dc3545; padding: 15px; margin: 20px 0; text-align: center; }}
        .otp-code {{ font-size: 24px; font-weight: bold; color: #dc3545; letter-spacing: 2px; }}
        .footer {{ color: #666; font-size: 12px; margin-top: 20px; border-top: 1px solid #ddd; padding-top: 10px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h2>Password Reset OTP</h2>
        </div>
        <div class=""content"">
            <p>Hi {userName},</p>
            <p>We received a request to reset your password for your Grocery Store account.</p>
            
            <div class=""otp-box"">
                <p><strong>Your One-Time Password (OTP) is:</strong></p>
                <div class=""otp-code"">{otp}</div>
                <p>This OTP will expire in 10 minutes.</p>
            </div>
            
            <p>Enter this OTP on the password reset page to continue.</p>
            <p><strong>If you didn't request this:</strong> You can safely ignore this email. Your password will remain unchanged.</p>
            
            <p>For security reasons, do not share this OTP with anyone.</p>
        </div>
        <div class=""footer"">
            <p>This is an automated email. Please do not reply to this email.</p>
            <p>&copy; 2026 Grocery Store. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

                return await SendEmailAsync(userEmail, "Password Reset OTP", body);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending password reset OTP email to {userEmail}: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> SendEmailAsync(string toEmail, string subject, string body, string? replyTo = null)
        {
            try
            {
                var emailSettings = _configuration.GetSection("Email");
                var smtpHost = emailSettings["SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.TryParse(emailSettings["SmtpPort"], out var port) ? port : 587;
                var senderEmail = emailSettings["SenderEmail"] ?? throw new InvalidOperationException("SenderEmail not configured");
                var senderPassword = emailSettings["SenderPassword"] ?? throw new InvalidOperationException("SenderPassword not configured");
                var senderName = emailSettings["SenderName"] ?? "Grocery Store";

                using (var client = new SmtpClient(smtpHost, smtpPort))
                {
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential(senderEmail, senderPassword);

                    using (var mailMessage = new MailMessage())
                    {
                        mailMessage.From = new MailAddress(senderEmail, senderName);
                        mailMessage.To.Add(toEmail);
                        mailMessage.Subject = subject;
                        mailMessage.Body = body;
                        mailMessage.IsBodyHtml = true;

                        if (!string.IsNullOrEmpty(replyTo))
                        {
                            mailMessage.ReplyToList.Add(new MailAddress(replyTo));
                        }

                        await client.SendMailAsync(mailMessage);
                        _logger.LogInformation($"Email sent successfully to {toEmail}");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending email to {toEmail}: {ex.Message}");
                return false;
            }
        }
    }
}

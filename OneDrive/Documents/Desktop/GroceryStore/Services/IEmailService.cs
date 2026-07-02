namespace GroceryStore.Services
{
    public interface IEmailService
    {
        Task<bool> SendOrderConfirmationEmailAsync(string userEmail, string userName, string orderNumber, decimal totalAmount, List<(string productName, int quantity, decimal price)> items);
        Task<bool> SendPaymentConfirmationEmailAsync(string userEmail, string userName, string orderNumber, string paymentMethod, decimal amount);
        Task<bool> SendPaymentFailureEmailAsync(string userEmail, string userName, string orderNumber, string errorMessage);
        Task<bool> SendOrderStatusEmailAsync(string userEmail, string userName, string orderNumber, string status);
        Task<bool> SendDeliveryUpdateEmailAsync(string userEmail, string userName, string orderNumber, string deliveryStatus, string? deliveryDate = null);
        Task<bool> SendWelcomeEmailAsync(string userEmail, string userName);
        Task<bool> SendContactFormEmailAsync(string senderName, string senderEmail, string subject, string message);
        Task<bool> SendPasswordResetOTPAsync(string userEmail, string userName, string otp);
    }
}

using Razorpay.Api;
using System.Security.Cryptography;
using System.Text;

namespace GroceryStore.Services
{
    public interface IRazorpayService
    {
        Dictionary<string, object> CreateOrder(int amountInCents, string orderId);
        bool VerifySignature(string razorpayPaymentId, string razorpayOrderId, string razorpaySignature);
        Payment GetPayment(string paymentId);
        string GetKeyId();
    }

    public class RazorpayService : IRazorpayService
    {
        private readonly IConfiguration _configuration;
        private readonly RazorpayClient _razorpayClient;
        private readonly string _keyId;
        private readonly string _keySecret;

        public RazorpayService(IConfiguration configuration)
        {
            _configuration = configuration;
            _keyId = _configuration["Razorpay:KeyId"] ?? throw new ArgumentNullException("Razorpay:KeyId");
            _keySecret = _configuration["Razorpay:KeySecret"] ?? throw new ArgumentNullException("Razorpay:KeySecret");

            _razorpayClient = new RazorpayClient(_keyId, _keySecret);
        }

        public string GetKeyId() => _keyId;
        /// Creates a Razorpay order for payment
        /// </summary>
        public Dictionary<string, object> CreateOrder(int amountInCents, string orderId)
        {
            try
            {
                Dictionary<string, object> orderData = new()
                {
                    { "amount", amountInCents }, // Amount in paise (1 rupee = 100 paise)
                    { "currency", "INR" },
                    { "receipt", orderId }
                };

                Order order = _razorpayClient.Order.Create(orderData);
                
                return new Dictionary<string, object>
                {
                    { "orderId", order["id"] },
                    { "amount", order["amount"] },
                    { "currency", order["currency"] },
                    { "status", order["status"] },
                    { "keyId", _keyId }
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create Razorpay order: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Verifies the Razorpay payment signature
        /// </summary>
        public bool VerifySignature(string razorpayPaymentId, string razorpayOrderId, string razorpaySignature)
        {
            try
            {
                string message = razorpayOrderId + "|" + razorpayPaymentId;
                
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                byte[] keyBytes = Encoding.UTF8.GetBytes(_keySecret);
                
                using (var hmac = new HMACSHA256(keyBytes))
                {
                    byte[] hashBytes = hmac.ComputeHash(messageBytes);
                    string computedSignature = Convert.ToHexString(hashBytes).ToLower();
                    
                    return computedSignature == razorpaySignature.ToLower();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Signature verification failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Fetches payment details from Razorpay
        /// </summary>
        public Payment GetPayment(string paymentId)
        {
            try
            {
                return _razorpayClient.Payment.Fetch(paymentId);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to fetch payment: {ex.Message}", ex);
            }
        }
    }
}

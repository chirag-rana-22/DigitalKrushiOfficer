namespace GroceryStore.ViewModels
{
    public class PaymentGatewayViewModel
    {
        public int OrderId { get; set; }
        public string? OrderNumber { get; set; }
        public int Amount { get; set; }
        public string? RazorpayOrderId { get; set; }
        public string? RazorpayKeyId { get; set; }
        public string? UserEmail { get; set; }
        public string? UserPhone { get; set; }
    }
}

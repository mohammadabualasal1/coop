namespace coop.Dtos.CheckoutController
{
    public class CheckoutSummaryResponse
    {
        public decimal Subtotal { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal TotalAmount { get; set; }
    }
}

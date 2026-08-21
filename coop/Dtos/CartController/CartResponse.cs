namespace coop.Dtos.CartController
{
    public class CartResponse
    {
        public Guid Id { get; set; }
        public Guid MerchantBranchId { get; set; }
        public List<CartItemResponse> Items { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal EstimatedTotal { get; set; }
    }
}

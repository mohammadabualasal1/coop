using coop.Enums;

namespace coop.Dtos.OrdersDtos
{
    public class OrderDetailResponseDto
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; }
        public OrderStatus Status { get; set; }
        public string MerchantName { get; set; }
        public string BranchName { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal TotalAmount { get; set; }
        public string? CustomerNotes { get; set; }
        public DateTime PlacedAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public DateTime? ReadyAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public List<OrderItemResponseDto> Items { get; set; }
    }
}

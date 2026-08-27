using coop.Enums;

namespace coop.Dtos.MerchantOrdersDtos
{
    public class MerchantOrderDetailResponseDto
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public OrderStatus Status { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal TotalAmount { get; set; }
        public string? CustomerNotes { get; set; }
        public DateTime PlacedAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public DateTime? ReadyAt { get; set; }
        public List<MerchantOrderItemResponseDto> Items { get; set; }
    }
}

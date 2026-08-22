using coop.Enums;

namespace coop.Dtos.MerchantOrdersController
{
    public class MerchantOrderResponse
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; }
        public string CustomerName { get; set; }
        public OrderStatus Status { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime PlacedAt { get; set; }
    }
}

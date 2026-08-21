using coop.Enums;

namespace coop.Dtos.OrdersController
{
    public class OrderStatusHistoryResponse
    {
        public OrderStatus OldStatus { get; set; }
        public OrderStatus NewStatus { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

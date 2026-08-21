using System.ComponentModel.DataAnnotations.Schema;

namespace coop.Model
{
    public class OrderStatusHistory
    {
        public Guid Id { get; set; }

        [ForeignKey("OrderId")]
        public Guid OrderId { get; set; }
        public Order Order { get; set; }

        public OrderStatus OldStatus { get; set; }
        public OrderStatus NewStatus { get; set; }

        [ForeignKey("ChangedByUserId")]
        public Guid ChangedByUserId { get; set; }
        public User ChangedByUser { get; set; }

        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

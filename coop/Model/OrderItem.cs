using System.ComponentModel.DataAnnotations.Schema;

namespace coop.Model
{
    public class OrderItem
    {
        public Guid Id { get; set; }

        [ForeignKey("OrderId")]
        public Guid OrderId { get; set; }
        public Order Order { get; set; }

        [ForeignKey("OfferId")]
        public Guid OfferId { get; set; }
        public Offer Offer { get; set; }

        [ForeignKey("ProductId")]
        public Guid ProductId { get; set; }
        public Product Product { get; set; }

        public string ProductNameSnapshot { get; set; }
        public decimal OriginalUnitPrice { get; set; }
        public decimal DiscountedUnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal LineSubtotal { get; set; }
        public decimal LineDiscount { get; set; }
        public decimal LineTotal { get; set; }
    }
}

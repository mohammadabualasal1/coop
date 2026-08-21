using System.ComponentModel.DataAnnotations.Schema;

namespace coop.Model
{
    public class Order
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; }

        [ForeignKey("CustomerUserId")]
        public Guid CustomerUserId { get; set; }
        public User CustomerUser { get; set; }

        [ForeignKey("MerchantId")]
        public Guid MerchantId { get; set; }
        public Merchant Merchant { get; set; }

        [ForeignKey("MerchantBranchId")]
        public Guid MerchantBranchId { get; set; }
        public MerchantBranch MerchantBranch { get; set; }

        [ForeignKey("CustomerAddressId")]
        public Guid CustomerAddressId { get; set; }
        public CustomerAddress CustomerAddress { get; set; }

        public OrderStatus Status { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal TotalAmount { get; set; }
        public string? CustomerNotes { get; set; }
        public string? MerchantRejectionReason { get; set; }
        public string? CancellationReason { get; set; }
        public DateTime PlacedAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public DateTime? ReadyAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

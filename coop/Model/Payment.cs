using System.ComponentModel.DataAnnotations.Schema;

namespace coop.Model
{
    public class Payment
    {
        public Guid Id { get; set; }

        [ForeignKey("OrderId")]
        public Guid OrderId { get; set; }
        public Order Order { get; set; }

        public PaymentMethod Method { get; set; }
        public PaymentStatus Status { get; set; }
        public decimal Amount { get; set; }
        public string MockProvider { get; set; }
        public string TransactionReference { get; set; }
        public string? FailureReason { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? RefundedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

using coop.Enums;

namespace coop.Dtos.PaymentsController
{
    public class PaymentResponse
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public PaymentMethod Method { get; set; }
        public PaymentStatus Status { get; set; }
        public decimal Amount { get; set; }
        public string TransactionReference { get; set; }
        public DateTime? PaidAt { get; set; }
    }
}

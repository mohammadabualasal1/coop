using System.ComponentModel.DataAnnotations.Schema;
using coop.Enums;

namespace coop.Model
{
    public class StockReservation
    {
        public Guid Id { get; set; }

        [ForeignKey("OrderId")]
        public Guid OrderId { get; set; }
        public Order Order { get; set; }

        [ForeignKey("BranchOfferId")]
        public Guid BranchOfferId { get; set; }
        public BranchOffer BranchOffer { get; set; }

        public int Quantity { get; set; }
        public StockReservationStatus Status { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReleasedAt { get; set; }
    }
}

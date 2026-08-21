using System.ComponentModel.DataAnnotations.Schema;
using coop.Enums;

namespace coop.Model
{
    public class DriverTaskOffer
    {
        public Guid Id { get; set; }

        [ForeignKey("DeliveryTaskId")]
        public Guid DeliveryTaskId { get; set; }
        public DeliveryTask DeliveryTask { get; set; }

        [ForeignKey("DriverProfileId")]
         public Guid DriverProfileId { get; set; }
        public DriverProfile DriverProfile { get; set; }

        public DriverTaskOfferStatus Status { get; set; }
        public decimal MatchScore { get; set; }
        public DateTime OfferedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public string? RejectionReason { get; set; }
    }
}

using System.ComponentModel.DataAnnotations.Schema;
using coop.Enums;

namespace coop.Model
{
    public class Complaint
    {
        public Guid Id { get; set; }

        [ForeignKey("CreatedByUserId")]
        public Guid CreatedByUserId { get; set; }
        public User CreatedByUser { get; set; }

        [ForeignKey("OrderId")]
        public Guid? OrderId { get; set; }
        public Order? Order { get; set; }

        [ForeignKey("MerchantId")]
        public Guid? MerchantId { get; set; }
        public Merchant? Merchant { get; set; }

        [ForeignKey("DriverProfileId")]
        public Guid? DriverProfileId { get; set; }
        public DriverProfile? DriverProfile { get; set; }

        [ForeignKey("OfferId")]
        public Guid? OfferId { get; set; }
        public Offer? Offer { get; set; }

        public string Category { get; set; }
        public string Description { get; set; }
        public string? EvidenceUrl { get; set; }
        public ComplaintStatus Status { get; set; }
        public string? AdminResponse { get; set; }

        [ForeignKey("ResolvedByUserId")]
        public Guid? ResolvedByUserId { get; set; }
        public User? ResolvedByUser { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}

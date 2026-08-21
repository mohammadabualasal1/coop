using System.ComponentModel.DataAnnotations.Schema;

namespace coop.Model
{
    public class VerificationDocument
    {
        public Guid Id { get; set; }

        [ForeignKey("MerchantId")]
        public Guid? MerchantId { get; set; }
        public Merchant? Merchant { get; set; }

        [ForeignKey("DriverProfileId")]
        public Guid? DriverProfileId { get; set; }
        public DriverProfile? DriverProfile { get; set; }

        public string DocumentType { get; set; }
        public string FileUrl { get; set; }
        public VerificationStatus Status { get; set; }
        public string? ReviewNote { get; set; }
        public DateTime UploadedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }

        [ForeignKey("ReviewedByUserId")]
        public Guid? ReviewedByUserId { get; set; }
        public User? ReviewedByUser { get; set; }
    }
}

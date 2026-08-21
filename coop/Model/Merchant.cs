using System.ComponentModel.DataAnnotations.Schema;

namespace coop.Model
{
    public class Merchant
    {
        public Guid Id { get; set; }

        [ForeignKey("OwnerUserId")]
        public Guid OwnerUserId { get; set; }
        public User OwnerUser { get; set; }

        public string Name { get; set; }
        public string? Description { get; set; }
        public string? RegistrationNumber { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public string? LogoUrl { get; set; }
        public string? CoverImageUrl { get; set; }
        public VerificationStatus VerificationStatus { get; set; }
        public string? RejectionReason { get; set; }
        public bool IsActive { get; set; }
        public decimal? AverageRating { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }

        [ForeignKey("VerifiedByUserId")]
        public Guid? VerifiedByUserId { get; set; }
        public User? VerifiedByUser { get; set; }
    }
}

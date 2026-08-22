using coop.Enums;

namespace coop.Dtos.MerchantsController
{
    public class MerchantProfileResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? RegistrationNumber { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public string? LogoUrl { get; set; }
        public string? CoverImageUrl { get; set; }
        public VerificationStatus VerificationStatus { get; set; }
        public bool IsActive { get; set; }
        public decimal? AverageRating { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

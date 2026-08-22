using coop.Enums;

namespace coop.Dtos.MerchantsController
{
    public class VerificationStatusResponseDto
    {
        public VerificationStatus VerificationStatus { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime? VerifiedAt { get; set; }
    }
}

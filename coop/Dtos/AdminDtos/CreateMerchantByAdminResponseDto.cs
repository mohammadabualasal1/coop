using coop.Enums;

namespace coop.Dtos.AdminController
{
    public class CreateMerchantByAdminResponseDto
    {
        public Guid MerchantId { get; set; }
        public Guid OwnerUserId { get; set; }
        public string MerchantName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public VerificationStatus VerificationStatus { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
using coop.Enums;

namespace coop.Dtos.AdminController
{
    public class CreateDriverByAdminResponseDto
    {
        public Guid DriverProfileId { get; set; }
        public Guid UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string VehicleType { get; set; }
        public string VehiclePlateNumber { get; set; }
        public int MaximumCapacity { get; set; }
        public VerificationStatus VerificationStatus { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
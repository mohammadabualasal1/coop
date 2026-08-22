using coop.Enums;

namespace coop.Dtos.AuthController
{
    public class CurrentUserResponseDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public UserRole Role { get; set; }
        public UserStatus Status { get; set; }
        public string? ProfileImageUrl { get; set; }
    }
}

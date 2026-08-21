using System.ComponentModel.DataAnnotations.Schema;

namespace coop.Model
{
    public class User
    {
        public Guid Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string PasswordHash { get; set; }
        public UserRole Role { get; set; }
        public UserStatus Status { get; set; }
        public string? ProfileImageUrl { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

    }
}

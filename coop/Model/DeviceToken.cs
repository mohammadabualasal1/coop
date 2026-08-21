using System.ComponentModel.DataAnnotations.Schema;

namespace coop.Model
{
    public class DeviceToken
    {
        public Guid Id { get; set; }

        [ForeignKey("UserId")]
        public Guid UserId { get; set; }
        public User User { get; set; }

        public string Token { get; set; }
        public DevicePlatform Platform { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
    }
}

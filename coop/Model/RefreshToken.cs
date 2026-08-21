using System.ComponentModel.DataAnnotations.Schema;
using coop.Enums;

namespace coop.Model
{
    public class RefreshToken
    {
        public Guid Id { get; set; }

        [ForeignKey("UserId")]
        public Guid UserId { get; set; }
        public User User { get; set; }

        public string TokenHash { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RevokedAt { get; set; }

        [ForeignKey("ReplacedByTokenId")]
        public Guid? ReplacedByTokenId { get; set; }
        public RefreshToken? ReplacedByToken { get; set; }

        public string? DeviceName { get; set; }
        public string? IpAddress { get; set; }
    }
}

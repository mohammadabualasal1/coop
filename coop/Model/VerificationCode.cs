using System.ComponentModel.DataAnnotations.Schema;

namespace coop.Model
{
    public class VerificationCode
    {
        public Guid Id { get; set; }

        [ForeignKey("UserId")]
        public Guid? UserId { get; set; }
        public User? User { get; set; }

        public string Destination { get; set; }
        public VerificationCodePurpose Purpose { get; set; }
        public string CodeHash { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? UsedAt { get; set; }
        public int AttemptCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

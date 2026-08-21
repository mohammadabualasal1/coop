using System.ComponentModel.DataAnnotations.Schema;

namespace coop.Model
{
    public class ConfirmationToken
    {
        public Guid Id { get; set; }

        [ForeignKey("DeliveryTaskId")]
        public Guid DeliveryTaskId { get; set; }
        public DeliveryTask DeliveryTask { get; set; }

        public ConfirmationTokenType Type { get; set; }
        public string TokenHash { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? UsedAt { get; set; }

        [ForeignKey("UsedByUserId")]
        public Guid? UsedByUserId { get; set; }
        public User? UsedByUser { get; set; }

        public bool IsRevoked { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

using System.ComponentModel.DataAnnotations.Schema;
using coop.Enums;

namespace coop.Model
{
    public class AuditLog
    {
        public Guid Id { get; set; }

        [ForeignKey("UserId")]
        public Guid UserId { get; set; }
        public User User { get; set; }

        public string Action { get; set; }
        public string EntityType { get; set; }
        public Guid EntityId { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

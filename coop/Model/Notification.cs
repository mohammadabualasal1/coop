using System.ComponentModel.DataAnnotations.Schema;
using coop.Enums;

namespace coop.Model
{
    public class Notification
    {
        public Guid Id { get; set; }

        [ForeignKey("UserId")]
        public Guid UserId { get; set; }
        public User User { get; set; }

        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; }
        public string? RelatedEntityType { get; set; }
        public Guid? RelatedEntityId { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
    }
}

namespace coop.Dtos.AdminController
{
    public class AuditLogResponse
    {
        public Guid Id { get; set; }
        public string Action { get; set; }
        public string EntityType { get; set; }
        public Guid EntityId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

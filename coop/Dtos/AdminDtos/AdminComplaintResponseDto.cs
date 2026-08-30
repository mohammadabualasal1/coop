using coop.Enums;

namespace coop.Dtos.AdminController
{
    public class AdminComplaintResponseDto
    {
        public Guid Id { get; set; }
        public string CreatedByName { get; set; }
        public string? OrderNumber { get; set; }
        public string? TargetName { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string? EvidenceUrl { get; set; }
        public ComplaintStatus Status { get; set; }
        public string? AdminResponse { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}
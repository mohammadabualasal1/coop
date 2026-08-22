namespace coop.Dtos.AdminController
{
    public class PendingVerificationResponseDto
    {
        public Guid Id { get; set; }
        public string EntityType { get; set; }
        public string EntityName { get; set; }
        public DateTime SubmittedAt { get; set; }
    }
}

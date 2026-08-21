namespace coop.Dtos.AdminController
{
    public class PendingVerificationResponse
    {
        public Guid Id { get; set; }
        public string EntityType { get; set; }
        public string EntityName { get; set; }
        public DateTime SubmittedAt { get; set; }
    }
}

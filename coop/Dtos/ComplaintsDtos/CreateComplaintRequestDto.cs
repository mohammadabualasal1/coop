namespace coop.Dtos.ComplaintsController
{
    public class CreateComplaintRequest
    {
        public Guid? OrderId { get; set; }
        public Guid? MerchantId { get; set; }
        public Guid? DriverProfileId { get; set; }
        public Guid? OfferId { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string? EvidenceUrl { get; set; }
    }
}

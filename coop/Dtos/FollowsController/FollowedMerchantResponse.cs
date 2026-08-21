namespace coop.Dtos.FollowsController
{
    public class FollowedMerchantResponse
    {
        public Guid Id { get; set; }
        public Guid MerchantId { get; set; }
        public string Name { get; set; }
        public string? LogoUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

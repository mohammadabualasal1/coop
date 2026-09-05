namespace coop.Dtos.FollowsController
{
    public class FollowedMerchantResponseDto
    {
        public Guid Id { get; set; }
        public Guid MerchantId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public decimal? AverageRating { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

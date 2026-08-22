namespace coop.Dtos.MarketplaceController
{
    public class MerchantSummaryResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? LogoUrl { get; set; }
        public decimal? AverageRating { get; set; }
    }
}

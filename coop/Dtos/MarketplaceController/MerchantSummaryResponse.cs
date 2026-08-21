namespace coop.Dtos.MarketplaceController
{
    public class MerchantSummaryResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? LogoUrl { get; set; }
        public decimal? AverageRating { get; set; }
    }
}

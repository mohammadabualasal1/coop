namespace coop.Dtos.MarketplaceController
{
    public class MerchantSearchRequestDto
    {
        public string? Search { get; set; }
        public string? City { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
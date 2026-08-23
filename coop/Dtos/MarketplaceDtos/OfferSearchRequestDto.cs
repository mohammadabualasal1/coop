namespace coop.Dtos.MarketplaceController
{
    public class OfferSearchRequestDto
    {
        public string? Search { get; set; }
        public Guid? CategoryId { get; set; }
        public Guid? MerchantId { get; set; }
        public string? City { get; set; }
        public decimal? MinimumDiscount { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? SortBy { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
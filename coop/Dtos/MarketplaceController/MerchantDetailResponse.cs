using System.Collections.Generic;
namespace coop.Dtos.MarketplaceController
{
    public class MerchantDetailResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public string? CoverImageUrl { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public decimal? AverageRating { get; set; }
        public List<MerchantBranchSummaryResponse> Branches { get; set; }
    }
}

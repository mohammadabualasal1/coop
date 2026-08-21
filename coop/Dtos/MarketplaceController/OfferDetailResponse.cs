using coop.Enums;
using System.Collections.Generic;
namespace coop.Dtos.MarketplaceController
{
    public class OfferDetailResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public Guid ProductId { get; set; }
        public Guid MerchantId { get; set; }
        public string MerchantName { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal DiscountedPrice { get; set; }
        public decimal DiscountPercentage { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public OfferStatus Status { get; set; }
        public int? MaximumQuantityPerCustomer { get; set; }
        public List<BranchStockResponse> Branches { get; set; }
    }
}

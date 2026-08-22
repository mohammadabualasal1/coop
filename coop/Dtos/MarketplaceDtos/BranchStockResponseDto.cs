namespace coop.Dtos.MarketplaceController
{
    public class BranchStockResponse
    {
        public Guid MerchantBranchId { get; set; }
        public string BranchName { get; set; }
        public string City { get; set; }
        public int TotalStock { get; set; }
        public int AvailableStock { get; set; }
        public bool IsAvailable { get; set; }
    }
}

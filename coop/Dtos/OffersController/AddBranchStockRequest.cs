namespace coop.Dtos.OffersController
{
    public class AddBranchStockRequest
    {
        public Guid MerchantBranchId { get; set; }
        public int TotalStock { get; set; }
    }
}

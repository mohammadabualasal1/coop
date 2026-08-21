namespace coop.Dtos.OffersController
{
    public class BranchOfferResponse
    {
        public Guid Id { get; set; }
        public Guid MerchantBranchId { get; set; }
        public int TotalStock { get; set; }
        public int ReservedStock { get; set; }
        public int SoldStock { get; set; }
        public bool IsAvailable { get; set; }
    }
}

namespace coop.Dtos.DriverTaskOffersController
{
    public class DriverTaskOfferResponse
    {
        public Guid Id { get; set; }
        public Guid DeliveryTaskId { get; set; }
        public string MerchantBranchName { get; set; }
        public string CustomerCity { get; set; }
        public decimal DeliveryFee { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}

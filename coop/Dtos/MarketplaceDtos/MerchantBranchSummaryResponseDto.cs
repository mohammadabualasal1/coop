namespace coop.Dtos.MarketplaceController
{
    public class MerchantBranchSummaryResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string City { get; set; }
        public string Area { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}

namespace coop.Dtos.MerchantBranchesController
{
    public class UpdateBranchRequestDto
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Area { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string PhoneNumber { get; set; }
        public TimeOnly OpeningTime { get; set; }
        public TimeOnly ClosingTime { get; set; }
        public decimal DeliveryRadiusKm { get; set; }
        public decimal MinimumOrderAmount { get; set; }
        public decimal BaseDeliveryFee { get; set; }
    }
}

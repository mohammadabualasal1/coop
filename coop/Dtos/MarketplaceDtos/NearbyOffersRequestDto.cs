namespace coop.Dtos.MarketplaceController
{
    public class NearbyOffersRequestDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double RadiusKm { get; set; } = 10;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
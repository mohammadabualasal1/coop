namespace coop.Dtos.AddressesController
{
    public class CreateAddressRequest
    {
        public string Label { get; set; }
        public string ContactName { get; set; }
        public string ContactPhone { get; set; }
        public string City { get; set; }
        public string Area { get; set; }
        public string Street { get; set; }
        public string? Building { get; set; }
        public string? Floor { get; set; }
        public string? AdditionalDirections { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}

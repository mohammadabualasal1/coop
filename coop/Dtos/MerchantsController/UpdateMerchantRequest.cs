namespace coop.Dtos.MerchantsController
{
    public class UpdateMerchantRequest
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? RegistrationNumber { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public string? LogoUrl { get; set; }
        public string? CoverImageUrl { get; set; }
    }
}

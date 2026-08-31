namespace coop.Dtos.AdminController
{
    public class CreateMerchantByAdminRequestDto
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Password { get; set; }
        public string MerchantName { get; set; }
        public string? Description { get; set; }
        public string? RegistrationNumber { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public string? LogoUrl { get; set; }
        public string? CoverImageUrl { get; set; }
    }
}
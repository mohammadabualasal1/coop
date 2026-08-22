namespace coop.Dtos.AuthController
{
    public class UpdateProfileRequestDto
    {
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string? ProfileImageUrl { get; set; }
    }
}

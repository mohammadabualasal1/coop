using coop.Enums;

namespace coop.Dtos.AuthController
{
    public class SendVerificationCodeRequestDto
    {
        public string Destination { get; set; }
        public VerificationCodePurpose Purpose { get; set; }
    }
}

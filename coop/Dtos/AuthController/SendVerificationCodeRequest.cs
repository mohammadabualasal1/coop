using coop.Enums;

namespace coop.Dtos.AuthController
{
    public class SendVerificationCodeRequest
    {
        public string Destination { get; set; }
        public VerificationCodePurpose Purpose { get; set; }
    }
}

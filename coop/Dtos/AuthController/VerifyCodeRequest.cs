using coop.Enums;

namespace coop.Dtos.AuthController
{
    public class VerifyCodeRequest
    {
        public string Destination { get; set; }
        public string Code { get; set; }
        public VerificationCodePurpose Purpose { get; set; }
    }
}

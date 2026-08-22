using coop.Enums;

namespace coop.Dtos.DeviceTokensController
{
    public class RegisterDeviceTokenRequest
    {
        public string Token { get; set; }
        public DevicePlatform Platform { get; set; }
    }
}

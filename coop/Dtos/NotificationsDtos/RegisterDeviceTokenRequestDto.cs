using coop.Enums;

namespace coop.Dtos.NotificationsDtos
{
    public class RegisterDeviceTokenRequestDto
    {
        public string Token { get; set; }
        public DevicePlatform Platform { get; set; }
    }
}

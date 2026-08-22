using coop.Enums;

namespace coop.Dtos.DeviceTokensController
{
    public class DeviceTokenResponseDto
    {
        public Guid Id { get; set; }
        public string Token { get; set; }
        public DevicePlatform Platform { get; set; }
        public bool IsActive { get; set; }
    }
}

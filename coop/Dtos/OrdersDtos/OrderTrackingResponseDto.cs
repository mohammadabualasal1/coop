using coop.Enums;

namespace coop.Dtos.OrdersDtos
{
    public class OrderTrackingResponse
    {
        public DeliveryStatus Status { get; set; }
        public string? DriverName { get; set; }
        public double? DriverLatitude { get; set; }
        public double? DriverLongitude { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
    }
}

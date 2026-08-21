using coop.Enums;

namespace coop.Dtos.AdminController
{
    public class AdminDeliveryResponse
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; }
        public string? DriverName { get; set; }
        public DeliveryStatus Status { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}

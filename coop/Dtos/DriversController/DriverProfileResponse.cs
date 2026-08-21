using coop.Enums;

namespace coop.Dtos.DriversController
{
    public class DriverProfileResponse
    {
        public Guid Id { get; set; }
        public string VehicleType { get; set; }
        public string VehiclePlateNumber { get; set; }
        public int MaximumCapacity { get; set; }
        public VerificationStatus VerificationStatus { get; set; }
        public bool IsAvailable { get; set; }
        public decimal? AverageRating { get; set; }
        public int CompletedDeliveries { get; set; }
    }
}

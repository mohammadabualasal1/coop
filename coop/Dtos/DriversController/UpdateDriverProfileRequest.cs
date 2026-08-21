namespace coop.Dtos.DriversController
{
    public class UpdateDriverProfileRequest
    {
        public string VehicleType { get; set; }
        public string VehiclePlateNumber { get; set; }
        public int MaximumCapacity { get; set; }
    }
}

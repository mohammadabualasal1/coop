namespace coop.Dtos.DriversController
{
    public class UpdateDriverProfileRequestDto
    {
        public string VehicleType { get; set; }
        public string VehiclePlateNumber { get; set; }
        public int MaximumCapacity { get; set; }
    }
}

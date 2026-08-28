namespace coop.Dtos.DriversDtos
{
    public class CreateDriverProfileRequestDto
    {
        public string VehicleType { get; set; }
        public string VehiclePlateNumber { get; set; }
        public int MaximumCapacity { get; set; }
    }
}

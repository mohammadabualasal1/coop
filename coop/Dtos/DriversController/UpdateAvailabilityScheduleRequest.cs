namespace coop.Dtos.DriversController
{
    public class UpdateAvailabilityScheduleRequest
    {
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public bool IsActive { get; set; }
    }
}

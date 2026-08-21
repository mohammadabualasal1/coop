namespace coop.Dtos.DriversController
{
    public class CreateAvailabilityScheduleRequest
    {
        public int DayOfWeek { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
    }
}

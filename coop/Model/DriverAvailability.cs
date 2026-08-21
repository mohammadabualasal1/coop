using System.ComponentModel.DataAnnotations.Schema;
using coop.Enums;

namespace coop.Model
{
    public class DriverAvailability
    {
        public Guid Id { get; set; }

        [ForeignKey("DriverProfileId")]
        public Guid DriverProfileId { get; set; }
        public DriverProfile DriverProfile { get; set; }

        public int DayOfWeek { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public bool IsActive { get; set; }
    }
}

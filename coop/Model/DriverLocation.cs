using System.ComponentModel.DataAnnotations.Schema;
using coop.Enums;

namespace coop.Model
{
    public class DriverLocation
    {
        public Guid Id { get; set; }

        [ForeignKey("DeliveryTaskId")]
        public Guid DeliveryTaskId { get; set; }
        public DeliveryTask DeliveryTask { get; set; }

        [ForeignKey("DriverProfileId")]
        public Guid DriverProfileId { get; set; }
        public DriverProfile DriverProfile { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime RecordedAt { get; set; }
    }
}

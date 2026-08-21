using coop.Enums;

namespace coop.Dtos.DeliveryTasksController
{
    public class DeliveryTimelineItemResponse
    {
        public DeliveryStatus Status { get; set; }
        public DateTime OccurredAt { get; set; }
    }
}

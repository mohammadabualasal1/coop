using coop.Enums;

namespace coop.Dtos.DeliveryTasksController
{
    public class DeliveryTaskResponse
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public DeliveryStatus Status { get; set; }
        public Guid PickupBranchId { get; set; }
        public Guid CustomerAddressId { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal DriverEarning { get; set; }
    }
}

using System.ComponentModel.DataAnnotations.Schema;

namespace coop.Model
{
    public class DeliveryTask
    {
        public Guid Id { get; set; }

        [ForeignKey("OrderId")]
        public Guid OrderId { get; set; }
        public Order Order { get; set; }

        [ForeignKey("DriverProfileId")]
        public Guid? DriverProfileId { get; set; }
        public DriverProfile? DriverProfile { get; set; }

        [ForeignKey("PickupBranchId")]
        public Guid PickupBranchId { get; set; }
        public MerchantBranch PickupBranch { get; set; }

        [ForeignKey("CustomerAddressId")]
        public Guid CustomerAddressId { get; set; }
        public CustomerAddress CustomerAddress { get; set; }

        public DeliveryStatus Status { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal DriverEarning { get; set; }
        public DateTime? AssignedAt { get; set; }
        public DateTime? ArrivedAtMerchantAt { get; set; }
        public DateTime? PickedUpAt { get; set; }
        public DateTime? ArrivedAtCustomerAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public string? FailureReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

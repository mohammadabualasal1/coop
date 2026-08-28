using coop.Enums;

namespace coop.Dtos.DeliveryTasksController
{
    public class DeliveryTaskDetailResponseDto
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public string OrderNumber { get; set; }
        public DeliveryStatus Status { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal DriverEarning { get; set; }

        public string BranchName { get; set; }
        public string BranchAddress { get; set; }
        public string BranchPhone { get; set; }
        public double BranchLatitude { get; set; }
        public double BranchLongitude { get; set; }

        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerCity { get; set; }
        public string CustomerArea { get; set; }
        public string CustomerStreet { get; set; }
        public string? CustomerBuilding { get; set; }
        public string? CustomerFloor { get; set; }
        public string? AdditionalDirections { get; set; }
        public double CustomerLatitude { get; set; }
        public double CustomerLongitude { get; set; }

        public PaymentMethod PaymentMethod { get; set; }
        public decimal AmountToCollect { get; set; }

        public DateTime? AssignedAt { get; set; }
        public DateTime? ArrivedAtMerchantAt { get; set; }
        public DateTime? PickedUpAt { get; set; }
        public DateTime? ArrivedAtCustomerAt { get; set; }
    }
}
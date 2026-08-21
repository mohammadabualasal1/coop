namespace coop.Dtos.DriversController
{
    public class DeliveryHistoryItemResponse
    {
        public Guid DeliveryTaskId { get; set; }
        public string OrderNumber { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public decimal Earning { get; set; }
    }
}

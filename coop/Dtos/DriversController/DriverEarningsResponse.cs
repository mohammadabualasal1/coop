namespace coop.Dtos.DriversController
{
    public class DriverEarningsResponse
    {
        public decimal TotalEarnings { get; set; }
        public int CompletedDeliveries { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }
}

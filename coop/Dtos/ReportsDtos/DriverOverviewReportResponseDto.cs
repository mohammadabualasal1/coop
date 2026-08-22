namespace coop.Dtos.ReportsController
{
    public class DriverOverviewReportResponseDto
    {
        public int CompletedDeliveries { get; set; }
        public int FailedDeliveries { get; set; }
        public decimal AcceptanceRate { get; set; }
        public decimal AverageDeliveryDurationMinutes { get; set; }
        public decimal TotalEarnings { get; set; }
    }
}

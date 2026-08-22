namespace coop.Dtos.ReportsController
{
    public class AdminDeliveryReportResponseDto
    {
        public decimal AverageAssignmentTimeMinutes { get; set; }
        public decimal AverageDeliveryDurationMinutes { get; set; }
        public decimal FailureRate { get; set; }
        public decimal CancellationRate { get; set; }
    }
}

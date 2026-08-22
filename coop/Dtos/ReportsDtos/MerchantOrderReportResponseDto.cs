namespace coop.Dtos.ReportsController
{
    public class MerchantOrderReportResponse
    {
        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
    }
}

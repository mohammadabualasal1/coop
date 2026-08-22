namespace coop.Dtos.ReportsController
{
    public class AdminSalesReportResponseDto
    {
        public int TotalOrders { get; set; }
        public decimal GrossSales { get; set; }
        public int CashOnDeliveryOrders { get; set; }
        public int MockOnlinePaymentOrders { get; set; }
    }
}

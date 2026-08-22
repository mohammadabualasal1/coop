namespace coop.Dtos.ReportsController
{
    public class MerchantOverviewReportResponse
    {
        public int ActiveOffers { get; set; }
        public int ExpiredOffers { get; set; }
        public int SoldOutOffers { get; set; }
        public int TotalOrders { get; set; }
        public decimal GrossSales { get; set; }
        public decimal TotalDiscountGiven { get; set; }
    }
}

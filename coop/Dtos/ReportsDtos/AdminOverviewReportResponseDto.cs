namespace coop.Dtos.ReportsController
{
    public class AdminOverviewReportResponse
    {
        public int ActiveUsers { get; set; }
        public int ActiveMerchants { get; set; }
        public int ActiveDrivers { get; set; }
        public int ActiveOffers { get; set; }
        public int PendingVerifications { get; set; }
        public int PendingOfferApprovals { get; set; }
    }
}

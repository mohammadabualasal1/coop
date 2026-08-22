namespace coop.Dtos.ReportsController
{
    public class MerchantOfferReportItemDto
    {
        public Guid OfferId { get; set; }
        public string Title { get; set; }
        public int Views { get; set; }
        public int Favorites { get; set; }
        public int OrdersCount { get; set; }
        public int QuantitySold { get; set; }
        public decimal GrossSales { get; set; }
    }
}

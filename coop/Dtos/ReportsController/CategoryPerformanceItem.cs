namespace coop.Dtos.ReportsController
{
    public class CategoryPerformanceItem
    {
        public Guid CategoryId { get; set; }
        public string NameEn { get; set; }
        public int OrdersCount { get; set; }
        public decimal GrossSales { get; set; }
    }
}

namespace coop.Dtos.AdminController
{
    public class PendingOfferResponseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string MerchantName { get; set; }
        public decimal DiscountPercentage { get; set; }
        public DateTime SubmittedAt { get; set; }
    }
}

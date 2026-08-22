namespace coop.Dtos.OffersController
{
    public class UpdateOfferRequestDto
    {
        public string Title { get; set; }
        public string? Description { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal DiscountedPrice { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public int? MaximumQuantityPerCustomer { get; set; }
    }
}

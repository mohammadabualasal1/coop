namespace coop.Dtos.FavoritesController
{
    public class FavoriteOfferResponse
    {
        public Guid Id { get; set; }
        public Guid OfferId { get; set; }
        public string Title { get; set; }
        public Guid MerchantId { get; set; }
        public string MerchantName { get; set; }
        public string? MainImageUrl { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal DiscountedPrice { get; set; }
        public decimal DiscountPercentage { get; set; }
        public DateTime EndAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

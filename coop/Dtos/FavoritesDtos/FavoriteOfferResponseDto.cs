namespace coop.Dtos.FavoritesController
{
    public class FavoriteOfferResponse
    {
        public Guid Id { get; set; }
        public Guid OfferId { get; set; }
        public string Title { get; set; }
        public decimal DiscountedPrice { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

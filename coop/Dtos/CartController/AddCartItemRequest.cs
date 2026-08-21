namespace coop.Dtos.CartController
{
    public class AddCartItemRequest
    {
        public Guid OfferId { get; set; }
        public int Quantity { get; set; }
    }
}

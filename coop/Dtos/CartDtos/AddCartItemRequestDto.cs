namespace coop.Dtos.CartController
{
    public class AddCartItemRequestDto
    {
        public Guid OfferId { get; set; }
        public int Quantity { get; set; }
    }
}

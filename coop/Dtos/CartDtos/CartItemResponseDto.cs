namespace coop.Dtos.CartController
{
    public class CartItemResponse
    {
        public Guid Id { get; set; }
        public Guid OfferId { get; set; }
        public string Title { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }
}

namespace coop.Dtos.CartController
{
    public class CartValidationResponse
    {
        public bool IsValid { get; set; }
        public List<string> Issues { get; set; }
        public CartResponse Cart { get; set; }
    }
}

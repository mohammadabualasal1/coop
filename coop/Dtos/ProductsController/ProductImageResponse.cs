namespace coop.Dtos.ProductsController
{
    public class ProductImageResponse
    {
        public Guid Id { get; set; }
        public string FileUrl { get; set; }
        public int DisplayOrder { get; set; }
    }
}

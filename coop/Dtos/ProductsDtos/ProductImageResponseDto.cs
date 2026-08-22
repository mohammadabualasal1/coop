namespace coop.Dtos.ProductsController
{
    public class ProductImageResponseDto
    {
        public Guid Id { get; set; }
        public string FileUrl { get; set; }
        public int DisplayOrder { get; set; }
    }
}

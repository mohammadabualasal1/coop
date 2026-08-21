namespace coop.Dtos.ProductsController
{
    public class UpdateProductRequest
    {
        public Guid CategoryId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string? Sku { get; set; }
        public string? Brand { get; set; }
        public string? MainImageUrl { get; set; }
        public bool IsActive { get; set; }
    }
}

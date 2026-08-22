using System.Collections.Generic;
namespace coop.Dtos.ProductsController
{
    public class ProductResponse
    {
        public Guid Id { get; set; }
        public Guid MerchantId { get; set; }
        public Guid CategoryId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string? Sku { get; set; }
        public string? Brand { get; set; }
        public string? MainImageUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<ProductImageResponseDto> Images { get; set; }
    }
}

using System.ComponentModel.DataAnnotations.Schema;
using coop.Enums;

namespace coop.Model
{
    public class Product
    {
        public Guid Id { get; set; }

        [ForeignKey("MerchantId")]
        public Guid MerchantId { get; set; }
        public Merchant Merchant { get; set; }

        [ForeignKey("CategoryId")]
        public Guid CategoryId { get; set; }
        public Category Category { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public string? Sku { get; set; }
        public string? Brand { get; set; }
        public string? MainImageUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

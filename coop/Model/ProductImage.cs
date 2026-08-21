using System.ComponentModel.DataAnnotations.Schema;
using coop.Enums;

namespace coop.Model
{
    public class ProductImage
    {
        public Guid Id { get; set; }

        [ForeignKey("ProductId")]
        public Guid ProductId { get; set; }
        public Product Product { get; set; }

        public string FileUrl { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

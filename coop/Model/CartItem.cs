using System.ComponentModel.DataAnnotations.Schema;
using coop.Enums;

namespace coop.Model
{
    public class CartItem
    {
        public Guid Id { get; set; }

        [ForeignKey("CartId")]
        public Guid CartId { get; set; }
        public Cart Cart { get; set; }

        [ForeignKey("OfferId")]
        public Guid OfferId { get; set; }
        public Offer Offer { get; set; }

        public int Quantity { get; set; }
        public decimal AddedUnitPrice { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

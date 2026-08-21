using System.ComponentModel.DataAnnotations.Schema;

namespace coop.Model
{
    public class Offer
    {
        public Guid Id { get; set; }

        [ForeignKey("ProductId")]
        public Guid ProductId { get; set; }
        public Product Product { get; set; }

        [ForeignKey("MerchantId")]
        public Guid MerchantId { get; set; }
        public Merchant Merchant { get; set; }

        public string Title { get; set; }
        public string? Description { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal DiscountedPrice { get; set; }
        public decimal DiscountPercentage { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public OfferStatus Status { get; set; }
        public int? MaximumQuantityPerCustomer { get; set; }
        public string? AdminReviewNote { get; set; }
        public DateTime? ApprovedAt { get; set; }

        [ForeignKey("ApprovedByUserId")]
        public Guid? ApprovedByUserId { get; set; }
        public User? ApprovedByUser { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

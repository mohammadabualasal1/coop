using System.ComponentModel.DataAnnotations.Schema;

namespace coop.Model
{
    public class Review
    {
        public Guid Id { get; set; }

        [ForeignKey("OrderId")]
        public Order Order { get; set; }

        [ForeignKey("CustomerUserId")]
        public User CustomerUser { get; set; }

        [ForeignKey("MerchantId")]
        public Guid MerchantId { get; set; }
        public Merchant Merchant { get; set; }

        [ForeignKey("DriverProfileId")]
        public Guid? DriverProfileId { get; set; }
        public DriverProfile? DriverProfile { get; set; }

        public int MerchantRating { get; set; }
        public int? DriverRating { get; set; }
        public string? Comment { get; set; }
        public ReviewStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

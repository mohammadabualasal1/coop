using System.ComponentModel.DataAnnotations.Schema;

namespace coop.Model
{
    public class FollowedMerchant
    {
        public Guid Id { get; set; }

        [ForeignKey("CustomerUserId")]
        public Guid CustomerUserId { get; set; }
        public User CustomerUser { get; set; }

        [ForeignKey("MerchantId")]
        public Guid MerchantId { get; set; }
        public Merchant Merchant { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}

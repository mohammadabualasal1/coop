using System.ComponentModel.DataAnnotations.Schema;
using coop.Enums;
namespace coop.Model
{
    public class Cart
    {
        public Guid Id { get; set; }

        [ForeignKey("CustomerUserId")]
        public Guid CustomerUserId { get; set; }
        public User CustomerUser { get; set; }

        [ForeignKey("MerchantBranchId")]
        public Guid MerchantBranchId { get; set; }
        public MerchantBranch MerchantBranch { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}

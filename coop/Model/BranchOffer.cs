using System.ComponentModel.DataAnnotations.Schema;
using coop.Enums;

namespace coop.Model
{
    public class BranchOffer
    {
        public Guid Id { get; set; }

        [ForeignKey("OfferId")]
        public Guid OfferId { get; set; }
        public Offer Offer { get; set; }

        [ForeignKey("MerchantBranchId")]
        public Guid MerchantBranchId { get; set; }
        public MerchantBranch MerchantBranch { get; set; }

        public int TotalStock { get; set; }
        public int ReservedStock { get; set; }
        public int SoldStock { get; set; }
        public bool IsAvailable { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

using System.ComponentModel.DataAnnotations.Schema;

namespace coop.Model
{
    public class FavoriteOffer
    {
        public Guid Id { get; set; }

        [ForeignKey("CustomerUserId")]
        public Guid CustomerUserId { get; set; }
        public User CustomerUser { get; set; }

        [ForeignKey("OfferId")]
        public Guid OfferId { get; set; }
        public Offer Offer { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}

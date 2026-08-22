using coop.Enums;

namespace coop.Dtos.ReviewsController
{
    public class ReviewResponse
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public Guid CustomerUserId { get; set; }
        public int MerchantRating { get; set; }
        public int? DriverRating { get; set; }
        public string? Comment { get; set; }
        public ReviewStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

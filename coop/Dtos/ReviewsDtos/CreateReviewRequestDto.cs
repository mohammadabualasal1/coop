namespace coop.Dtos.ReviewsController
{
    public class CreateReviewRequestDto
    {
        public Guid OrderId { get; set; }

        public int MerchantRating { get; set; }
        public int? DriverRating { get; set; }
        public string? Comment { get; set; }
    }
}

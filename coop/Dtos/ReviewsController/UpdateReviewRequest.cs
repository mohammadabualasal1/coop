namespace coop.Dtos.ReviewsController
{
    public class UpdateReviewRequest
    {
        public int MerchantRating { get; set; }
        public int? DriverRating { get; set; }
        public string? Comment { get; set; }
    }
}

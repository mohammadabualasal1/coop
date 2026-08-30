using coop.Dtos.ReviewsController;
using coop.Enums;
using coop.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace coop.Controllers
{
    [Route("api/reviews")]
    [ApiController]
    [Authorize(Roles = "Customer")]
    public class ReviewsController : ControllerBase
    {
        private readonly CoopDbContext _dbcontext;

        public ReviewsController(CoopDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }

        [HttpPost]
        public async Task<IActionResult> CreateReview(CreateReviewRequestDto dto)
        {
            var userId = GetCurrentUserId();
            var now = DateTime.UtcNow;

            if (dto.MerchantRating < 1 || dto.MerchantRating > 5)
                return BadRequest("تقييم التاجر يجب أن يكون بين 1 و 5");

            if (dto.DriverRating != null && (dto.DriverRating < 1 || dto.DriverRating > 5))
                return BadRequest("تقييم السائق يجب أن يكون بين 1 و 5");

            var order = await _dbcontext.Orders
                .FirstOrDefaultAsync(o => o.Id == dto.OrderId && o.CustomerUserId == userId);

            if (order == null)
                return NotFound("الطلب غير موجود");

            if (order.Status != OrderStatus.Completed && order.Status != OrderStatus.Delivered)
                return BadRequest("لا يمكن تقييم الطلب قبل استلامه");

            var alreadyReviewed = await _dbcontext.Reviews
                .AnyAsync(r => r.OrderId == order.Id && r.CustomerUserId == userId);

            if (alreadyReviewed)
                return BadRequest("تم تقييم هذا الطلب مسبقاً");

            var driverProfileId = await _dbcontext.DeliveryTasks
                .Where(t => t.OrderId == order.Id)
                .Select(t => t.DriverProfileId)
                .FirstOrDefaultAsync();

            var review = new Review
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                CustomerUserId = userId,
                MerchantId = order.MerchantId,
                DriverProfileId = driverProfileId,
                MerchantRating = dto.MerchantRating,
                DriverRating = driverProfileId == null ? null : dto.DriverRating,
                Comment = dto.Comment,
                Status = ReviewStatus.Visible,
                CreatedAt = now
            };

            _dbcontext.Reviews.Add(review);
            await _dbcontext.SaveChangesAsync();

            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.Id == order.MerchantId);

            if (merchant != null)
            {
                merchant.AverageRating = await _dbcontext.Reviews
                    .Where(r => r.MerchantId == merchant.Id && r.Status == ReviewStatus.Visible)
                    .AverageAsync(r => (decimal)r.MerchantRating);
            }

            if (driverProfileId != null && review.DriverRating != null)
            {
                var driverProfile = await _dbcontext.DriverProfiles
                    .FirstOrDefaultAsync(d => d.Id == driverProfileId);

                if (driverProfile != null)
                {
                    driverProfile.AverageRating = await _dbcontext.Reviews
                        .Where(r => r.DriverProfileId == driverProfile.Id
                                 && r.DriverRating != null
                                 && r.Status == ReviewStatus.Visible)
                        .AverageAsync(r => (decimal)r.DriverRating!.Value);
                }
            }

            await _dbcontext.SaveChangesAsync();

            return StatusCode(201, new ReviewResponse
            {
                Id = review.Id,
                OrderId = review.OrderId,
                CustomerUserId = review.CustomerUserId,
                MerchantRating = review.MerchantRating,
                DriverRating = review.DriverRating,
                Comment = review.Comment,
                Status = review.Status,
                CreatedAt = review.CreatedAt
            });
        }

        private Guid GetCurrentUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
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

            var customerName = await _dbcontext.Users
                .Where(u => u.Id == userId)
                .Select(u => u.FullName)
                .FirstAsync();

            return StatusCode(201, new ReviewResponse
            {
                Id = review.Id,
                OrderId = review.OrderId,
                CustomerName = customerName,
                MerchantRating = review.MerchantRating,
                DriverRating = review.DriverRating,
                Comment = review.Comment,
                Status = review.Status,
                CreatedAt = review.CreatedAt
            });
        }

        [AllowAnonymous]
        [HttpGet("merchant/{id}")]
        public async Task<IActionResult> GetMerchantReviews(Guid id, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            if (pageNumber < 1)
                pageNumber = 1;

            if (pageSize < 1 || pageSize > 100)
                pageSize = 20;

            var merchantExists = await _dbcontext.Merchants
                .AnyAsync(m => m.Id == id && m.IsActive && m.VerificationStatus == VerificationStatus.Approved);

            if (!merchantExists)
                return NotFound("التاجر غير موجود أو غير متاح");

            var reviews = await _dbcontext.Reviews
                .Where(r => r.MerchantId == id && r.Status == ReviewStatus.Visible)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new ReviewResponse
                {
                    Id = r.Id,
                    OrderId = r.OrderId,
                    CustomerName = r.CustomerUser.FullName,
                    MerchantRating = r.MerchantRating,
                    DriverRating = r.DriverRating,
                    Comment = r.Comment,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return Ok(reviews);
        }
        [HttpGet("my")]
        public async Task<IActionResult> GetMyReviews()
        {
            var userId = GetCurrentUserId();

            var reviews = await _dbcontext.Reviews
                .Where(r => r.CustomerUserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReviewResponse
                {
                    Id = r.Id,
                    OrderId = r.OrderId,
                    CustomerName = r.CustomerUser.FullName,
                    MerchantRating = r.MerchantRating,
                    DriverRating = r.DriverRating,
                    Comment = r.Comment,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return Ok(reviews);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateReview(Guid id, UpdateReviewRequestDto dto)
        {
            var userId = GetCurrentUserId();
            var now = DateTime.UtcNow;

            if (dto.MerchantRating < 1 || dto.MerchantRating > 5)
                return BadRequest("تقييم التاجر يجب أن يكون بين 1 و 5");

            if (dto.DriverRating != null && (dto.DriverRating < 1 || dto.DriverRating > 5))
                return BadRequest("تقييم السائق يجب أن يكون بين 1 و 5");

            var review = await _dbcontext.Reviews
                .FirstOrDefaultAsync(r => r.Id == id && r.CustomerUserId == userId);

            if (review == null)
                return NotFound("التقييم غير موجود");

            if (review.CreatedAt.AddHours(24) < now)
                return BadRequest("لا يمكن تعديل التقييم بعد مرور 24 ساعة");

            review.MerchantRating = dto.MerchantRating;
            review.DriverRating = review.DriverProfileId == null ? null : dto.DriverRating;
            review.Comment = dto.Comment;

            await _dbcontext.SaveChangesAsync();

            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.Id == review.MerchantId);

            if (merchant != null)
            {
                merchant.AverageRating = await _dbcontext.Reviews
                    .Where(r => r.MerchantId == merchant.Id && r.Status == ReviewStatus.Visible)
                    .AverageAsync(r => (decimal)r.MerchantRating);
            }

            if (review.DriverProfileId != null)
            {
                var driverProfile = await _dbcontext.DriverProfiles
                    .FirstOrDefaultAsync(d => d.Id == review.DriverProfileId);

                if (driverProfile != null)
                {
                    var hasDriverRatings = await _dbcontext.Reviews
                        .AnyAsync(r => r.DriverProfileId == driverProfile.Id
                                    && r.DriverRating != null
                                    && r.Status == ReviewStatus.Visible);

                    driverProfile.AverageRating = hasDriverRatings
                        ? await _dbcontext.Reviews
                            .Where(r => r.DriverProfileId == driverProfile.Id
                                     && r.DriverRating != null
                                     && r.Status == ReviewStatus.Visible)
                            .AverageAsync(r => (decimal)r.DriverRating!.Value)
                        : null;
                }
            }

            await _dbcontext.SaveChangesAsync();

            var customerName = await _dbcontext.Users
                .Where(u => u.Id == userId)
                .Select(u => u.FullName)
                .FirstAsync();

            return Ok(new ReviewResponse
            {
                Id = review.Id,
                OrderId = review.OrderId,
                CustomerName = customerName,
                MerchantRating = review.MerchantRating,
                DriverRating = review.DriverRating,
                Comment = review.Comment,
                Status = review.Status,
                CreatedAt = review.CreatedAt
            });
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReview(Guid id)
        {
            var userId = GetCurrentUserId();
            var now = DateTime.UtcNow;

            var review = await _dbcontext.Reviews
                .FirstOrDefaultAsync(r => r.Id == id && r.CustomerUserId == userId);

            if (review == null)
                return NotFound("التقييم غير موجود");

            if (review.CreatedAt.AddHours(24) < now)
                return BadRequest("لا يمكن حذف التقييم بعد مرور 24 ساعة");

            var merchantId = review.MerchantId;
            var driverProfileId = review.DriverProfileId;

            _dbcontext.Reviews.Remove(review);
            await _dbcontext.SaveChangesAsync();

            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.Id == merchantId);

            if (merchant != null)
            {
                var hasMerchantRatings = await _dbcontext.Reviews
                    .AnyAsync(r => r.MerchantId == merchantId && r.Status == ReviewStatus.Visible);

                merchant.AverageRating = hasMerchantRatings
                    ? await _dbcontext.Reviews
                        .Where(r => r.MerchantId == merchantId && r.Status == ReviewStatus.Visible)
                        .AverageAsync(r => (decimal)r.MerchantRating)
                    : null;
            }

            if (driverProfileId != null)
            {
                var driverProfile = await _dbcontext.DriverProfiles
                    .FirstOrDefaultAsync(d => d.Id == driverProfileId);

                if (driverProfile != null)
                {
                    var hasDriverRatings = await _dbcontext.Reviews
                        .AnyAsync(r => r.DriverProfileId == driverProfileId
                                    && r.DriverRating != null
                                    && r.Status == ReviewStatus.Visible);

                    driverProfile.AverageRating = hasDriverRatings
                        ? await _dbcontext.Reviews
                            .Where(r => r.DriverProfileId == driverProfileId
                                     && r.DriverRating != null
                                     && r.Status == ReviewStatus.Visible)
                            .AverageAsync(r => (decimal)r.DriverRating!.Value)
                        : null;
                }
            }

            await _dbcontext.SaveChangesAsync();

            return NoContent();
        }
        private Guid GetCurrentUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
using System.Security.Claims;
using coop.Dtos.FollowsController;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using coop.Enums;
using coop.Model;
namespace coop.Controllers
{
    [ApiController]
    [Route("api/follows")]
    [Authorize(Roles = "Customer")]
    public class FollowsController : ControllerBase
    {
        private CoopDbContext _dbcontext;

        public FollowsController(CoopDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyFollowedMerchants()
        {
            var userId = GetCurrentUserId();

            var follows = await _dbcontext.FollowedMerchants
                .Where(f => f.CustomerUserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new FollowedMerchantResponseDto
                {
                    Id = f.Id,
                    MerchantId = f.MerchantId,
                    Name = f.Merchant.Name,
                    LogoUrl = f.Merchant.LogoUrl,
                    CreatedAt = f.CreatedAt
                })
                .ToListAsync();

            return Ok(follows);
        }
        [HttpPost("{merchantId}")]
        public async Task<IActionResult> FollowMerchant(Guid merchantId)
        {
            var userId = GetCurrentUserId();

            var merchant = await _dbcontext.Merchants
                .FirstOrDefaultAsync(m => m.Id == merchantId
                                       && m.IsActive
                                       && m.VerificationStatus == VerificationStatus.Approved);

            if (merchant == null)
                return NotFound("التاجر غير موجود أو غير متاح");

            var alreadyFollowed = await _dbcontext.FollowedMerchants
                .AnyAsync(f => f.CustomerUserId == userId && f.MerchantId == merchantId);

            if (alreadyFollowed)
                return BadRequest("أنت تتابع هذا التاجر بالفعل");

            var follow = new FollowedMerchant
            {
                Id = Guid.NewGuid(),
                CustomerUserId = userId,
                MerchantId = merchantId,
                CreatedAt = DateTime.UtcNow
            };

            _dbcontext.FollowedMerchants.Add(follow);
            await _dbcontext.SaveChangesAsync();

            return Ok(new FollowedMerchantResponseDto
            {
                Id = follow.Id,
                MerchantId = follow.MerchantId,
                Name = merchant.Name,
                LogoUrl = merchant.LogoUrl,
                CreatedAt = follow.CreatedAt
            });
        }

        private Guid GetCurrentUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
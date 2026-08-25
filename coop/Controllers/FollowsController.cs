using System.Security.Claims;
using coop.Dtos.FollowsController;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        private Guid GetCurrentUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
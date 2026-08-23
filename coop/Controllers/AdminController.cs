using coop.Dtos.AdminController;
using coop.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace coop.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly CoopDbContext _dbcontext;

        public AdminController(CoopDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }

        [HttpGet("verifications")]
        public async Task<IActionResult> GetPendingVerifications()
        {
            var userId = GetCurrentUserId();

            var pendingMerchants = await _dbcontext.Merchants
                .Where(m => m.VerificationStatus == VerificationStatus.Pending)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new PendingVerificationResponseDto
                {
                    Id = m.Id,
                    EntityType = "Merchant",
                    EntityName = m.Name,
                    SubmittedAt = m.CreatedAt
                })
                .ToListAsync();

            return Ok(pendingMerchants);
        }
        [HttpPost("merchants/{id}/approve")]
        public async Task<IActionResult> ApproveMerchant(Guid id)
        {
            var adminId = GetCurrentUserId();

            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.Id == id);
            if (merchant == null)
            {
                return NotFound("التاجر غير موجود");
            }
            merchant.VerificationStatus = VerificationStatus.Approved;
            merchant.VerifiedAt = DateTime.UtcNow;
            merchant.VerifiedByUserId = adminId;
            merchant.RejectionReason = null;

            await _dbcontext.SaveChangesAsync();
            return Ok(merchant);

        }

        private Guid GetCurrentUserId() =>
           Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
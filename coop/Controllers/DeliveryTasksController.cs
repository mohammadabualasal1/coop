using coop.Dtos.DriverTaskOffersController;
using coop.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace coop.Controllers
{
    [Route("api/delivery-tasks")]
    [ApiController]
    [Authorize(Roles = "Driver")]
    public class DeliveryTasksController : ControllerBase
    {
        private readonly CoopDbContext _dbcontext;

        public DeliveryTasksController(CoopDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }

        [HttpGet("offers")]
        public async Task<IActionResult> GetAvailableOffers()
        {
            var userId = GetCurrentUserId();
            var now = DateTime.UtcNow;

            var driverProfile = await _dbcontext.DriverProfiles
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (driverProfile == null)
                return NotFound("لا يوجد بروفايل سائق مرتبط بحسابك");

            if (driverProfile.VerificationStatus != VerificationStatus.Approved)
                return BadRequest("يجب توثيق حسابك قبل استقبال المهام");

            var offers = await _dbcontext.DriverTaskOffers
                .Where(o => o.DriverProfileId == driverProfile.Id)
                .Where(o => o.Status == DriverTaskOfferStatus.Pending)
                .Where(o => o.ExpiresAt > now)
                .OrderBy(o => o.ExpiresAt)
                .Select(o => new DriverTaskOfferResponseDto
                {
                    Id = o.Id,
                    DeliveryTaskId = o.DeliveryTaskId,
                    MerchantBranchName = o.DeliveryTask.PickupBranch.Name,
                    CustomerCity = o.DeliveryTask.CustomerAddress.City,
                    DeliveryFee = o.DeliveryTask.DeliveryFee,
                    ExpiresAt = o.ExpiresAt
                })
                .ToListAsync();

            return Ok(offers);
        }

        private Guid GetCurrentUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
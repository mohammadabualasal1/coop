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
        private CoopDbContext _dbcontext;

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
        [HttpPost("merchants/{id}/reject")]
        public async Task<IActionResult> RejectMerchant(Guid id, RejectRequestDto dto)
        {
            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.Id == id);
            if (merchant == null)
            {
                return NotFound("التاجر غير موجود");
            }

            merchant.VerificationStatus = VerificationStatus.Rejected;
            merchant.RejectionReason = dto.Reason;
            merchant.VerifiedAt = null;
            merchant.VerifiedByUserId = null;

            await _dbcontext.SaveChangesAsync();
            return Ok(merchant);
        }
        [HttpGet("offers/pending")]
        public async Task<IActionResult> GetPendingOffers()
        {
            var offers = await _dbcontext.Offers
                .Where(o => o.Status == OfferStatus.PendingApproval)
                .OrderBy(o => o.UpdatedAt)
                .Select(o => new PendingOfferResponseDto
                {
                    Id = o.Id,
                    Title = o.Title,
                    MerchantName = o.Merchant.Name,
                    DiscountPercentage = o.DiscountPercentage,
                    SubmittedAt = o.UpdatedAt
                })
                .ToListAsync();

            return Ok(offers);
        }
        [HttpPost("offers/{id}/approve")]
        public async Task<IActionResult> ApproveOffer(Guid id)
        {
            var adminId = GetCurrentUserId();

            var offer = await _dbcontext.Offers.FirstOrDefaultAsync(o => o.Id == id);
            if (offer == null)
                return NotFound("العرض غير موجود");

            var now = DateTime.UtcNow;

            offer.Status = offer.StartAt <= now && offer.EndAt >= now
                ? OfferStatus.Active
                : OfferStatus.Scheduled;

            offer.ApprovedAt = now;
            offer.ApprovedByUserId = adminId;
            offer.AdminReviewNote = null;
            offer.UpdatedAt = now;

            await _dbcontext.SaveChangesAsync();

            return Ok(offer);
        }

        [HttpPost("offers/{id}/reject")]
        public async Task<IActionResult> RejectOffer(Guid id, RejectRequestDto dto)
        {
            var offer = await _dbcontext.Offers.FirstOrDefaultAsync(o => o.Id == id);
            if (offer == null)
                return NotFound("العرض غير موجود");

            offer.Status = OfferStatus.Rejected;
            offer.AdminReviewNote = dto.Reason;
            offer.ApprovedAt = null;
            offer.ApprovedByUserId = null;
            offer.UpdatedAt = DateTime.UtcNow;

            await _dbcontext.SaveChangesAsync();

            return Ok(offer);
        }
        [HttpGet("drivers/pending")]
        public async Task<IActionResult> GetPendingDrivers()
        {
            var pendingDrivers = await _dbcontext.DriverProfiles
                .Where(d => d.VerificationStatus == VerificationStatus.Pending)
                .OrderBy(d => d.CreatedAt)
                .Select(d => new PendingVerificationResponseDto
                {
                    Id = d.Id,
                    EntityType = "Driver",
                    EntityName = d.User.FullName,
                    SubmittedAt = d.CreatedAt
                })
                .ToListAsync();

            return Ok(pendingDrivers);
        }

        [HttpPost("drivers/{id}/approve")]
        public async Task<IActionResult> ApproveDriver(Guid id)
        {
            var driverProfile = await _dbcontext.DriverProfiles.FirstOrDefaultAsync(d => d.Id == id);
            if (driverProfile == null)
                return NotFound("بروفايل السائق غير موجود");

            driverProfile.VerificationStatus = VerificationStatus.Approved;

            await _dbcontext.SaveChangesAsync();

            return Ok(new
            {
                driverProfile.Id,
                driverProfile.VerificationStatus
            });
        }
        [HttpPost("drivers/{id}/reject")]
        public async Task<IActionResult> RejectDriver(Guid id, RejectRequestDto dto)
        {
            var driverProfile = await _dbcontext.DriverProfiles.FirstOrDefaultAsync(d => d.Id == id);
            if (driverProfile == null)
                return NotFound("بروفايل السائق غير موجود");

            driverProfile.VerificationStatus = VerificationStatus.Rejected;
            driverProfile.RejectionReason = dto.Reason;
            driverProfile.IsAvailable = false;

            await _dbcontext.SaveChangesAsync();

            return Ok(new
            {
                driverProfile.Id,
                driverProfile.VerificationStatus,
                driverProfile.RejectionReason
            });
        }
        [HttpGet("complaints")]
        public async Task<IActionResult> GetAllComplaints([FromQuery] ComplaintStatus? status)
        {
            var query = _dbcontext.Complaints.AsQueryable();

            if (status != null)
                query = query.Where(c => c.Status == status);

            var complaints = await query
                .OrderBy(c => c.Status)
                .ThenBy(c => c.CreatedAt)
                .Select(c => new AdminComplaintResponseDto
                {
                    Id = c.Id,
                    CreatedByName = c.CreatedByUser.FullName,
                    OrderNumber = c.Order != null ? c.Order.OrderNumber : null,
                    TargetName = c.Merchant != null ? c.Merchant.Name
                               : c.DriverProfile != null ? c.DriverProfile.User.FullName
                               : c.Offer != null ? c.Offer.Title
                               : null,
                    Category = c.Category,
                    Description = c.Description,
                    EvidenceUrl = c.EvidenceUrl,
                    Status = c.Status,
                    AdminResponse = c.AdminResponse,
                    CreatedAt = c.CreatedAt,
                    ResolvedAt = c.ResolvedAt
                })
                .ToListAsync();

            return Ok(complaints);
        }


        private Guid GetCurrentUserId() =>
           Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
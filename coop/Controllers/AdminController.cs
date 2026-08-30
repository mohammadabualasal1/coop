using coop.Dtos.AdminController;
using coop.Enums;
using coop.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using coop.Services;
namespace coop.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly CoopDbContext _dbcontext;
        private readonly IAuditService _auditService;
        private readonly INotificationService _notificationService;

        public AdminController(CoopDbContext dbcontext, IAuditService auditService,INotificationService notificationService)
        {
            _dbcontext = dbcontext;
            _auditService = auditService;
            _notificationService = notificationService;

        }

        [HttpGet("verifications")]
        public async Task<IActionResult> GetPendingVerifications()
        {
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
                return NotFound("التاجر غير موجود");

            if (merchant.VerificationStatus == VerificationStatus.Approved)
                return BadRequest("التاجر موثّق بالفعل");

            merchant.VerificationStatus = VerificationStatus.Approved;
            merchant.VerifiedAt = DateTime.UtcNow;
            merchant.VerifiedByUserId = adminId;
            merchant.RejectionReason = null;

            await _dbcontext.SaveChangesAsync();
            await _auditService.LogAsync(adminId, "ApproveMerchant", "Merchant", merchant.Id,
                    $"تمت الموافقة على التاجر: {merchant.Name}");
            await _notificationService.NotifyAsync(
                merchant.OwnerUserId,
                "تمت الموافقة على متجرك",
                $"تم توثيق متجر {merchant.Name}، تقدر تضيف فروعك وعروضك الآن",
                "MerchantApproved",
                "Merchant",
                merchant.Id);

            return Ok(merchant);
        }

        [HttpPost("merchants/{id}/reject")]
        public async Task<IActionResult> RejectMerchant(Guid id, RejectRequestDto dto)
        {
            var adminId = GetCurrentUserId();
            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.Id == id);
            if (merchant == null)
                return NotFound("التاجر غير موجود");

            merchant.VerificationStatus = VerificationStatus.Rejected;
            merchant.RejectionReason = dto.Reason;
            merchant.VerifiedAt = null;
            merchant.VerifiedByUserId = null;

            await _dbcontext.SaveChangesAsync();
            await _auditService.LogAsync(adminId, "RejectMerchant", "Merchant", merchant.Id,
                    $"تم رفض التاجر: {merchant.Name} — السبب: {dto.Reason}");
            await _notificationService.NotifyAsync(
                merchant.OwnerUserId,
                "تم رفض طلب التوثيق",
                $"تم رفض توثيق متجر {merchant.Name}. السبب: {dto.Reason}",
                "MerchantRejected",
                "Merchant",
                merchant.Id);

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

            if (offer.Status != OfferStatus.PendingApproval)
                return BadRequest("العرض ليس قيد المراجعة");

            var now = DateTime.UtcNow;

            offer.Status = offer.StartAt <= now && offer.EndAt >= now
                ? OfferStatus.Active
                : OfferStatus.Scheduled;

            offer.ApprovedAt = now;
            offer.ApprovedByUserId = adminId;
            offer.AdminReviewNote = null;
            offer.UpdatedAt = now;

            await _dbcontext.SaveChangesAsync();
            await _auditService.LogAsync(adminId, "ApproveOffer", "Offer", offer.Id,
                $"تمت الموافقة على العرض: {offer.Title} — الحالة: {offer.Status}");
            var merchantOwnerUserId = await _dbcontext.Merchants
                .Where(m => m.Id == offer.MerchantId)
                .Select(m => m.OwnerUserId)
                .FirstAsync();

            await _notificationService.NotifyAsync(
                merchantOwnerUserId,
                "تمت الموافقة على عرضك",
                $"تمت الموافقة على العرض \"{offer.Title}\"",
                "OfferApproved",
                "Offer",
                offer.Id);

            return Ok(offer);
        }

        [HttpPost("offers/{id}/reject")]
        public async Task<IActionResult> RejectOffer(Guid id, RejectRequestDto dto)
        {
            var adminId = GetCurrentUserId();
            var offer = await _dbcontext.Offers.FirstOrDefaultAsync(o => o.Id == id);
            if (offer == null)
                return NotFound("العرض غير موجود");

            offer.Status = OfferStatus.Rejected;
            offer.AdminReviewNote = dto.Reason;
            offer.ApprovedAt = null;
            offer.ApprovedByUserId = null;
            offer.UpdatedAt = DateTime.UtcNow;

            await _dbcontext.SaveChangesAsync();
            await _auditService.LogAsync(adminId, "RejectOffer", "Offer", offer.Id,
                $"تم رفض العرض: {offer.Title} — السبب: {dto.Reason}");
            var merchantOwnerUserId = await _dbcontext.Merchants
                .Where(m => m.Id == offer.MerchantId)
                .Select(m => m.OwnerUserId)
                .FirstAsync();

            await _notificationService.NotifyAsync(
                merchantOwnerUserId,
                "تم رفض عرضك",
                $"تم رفض العرض \"{offer.Title}\". السبب: {dto.Reason}",
                "OfferRejected",
                "Offer",
                offer.Id);

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
            var adminId = GetCurrentUserId();
            var driverProfile = await _dbcontext.DriverProfiles.FirstOrDefaultAsync(d => d.Id == id);
            if (driverProfile == null)
                return NotFound("بروفايل السائق غير موجود");

            if (driverProfile.VerificationStatus == VerificationStatus.Approved)
                return BadRequest("السائق موثّق بالفعل");

            driverProfile.VerificationStatus = VerificationStatus.Approved;
            driverProfile.RejectionReason = null;

            await _dbcontext.SaveChangesAsync();
            await _auditService.LogAsync(adminId, "ApproveDriver", "DriverProfile", driverProfile.Id,
                    "تمت الموافقة على السائق");
            await _notificationService.NotifyAsync(
                driverProfile.UserId,
                "تمت الموافقة على حسابك",
                "تم توثيق حسابك كسائق، تقدر تبدأ استقبال مهام التوصيل",
                "DriverApproved",
                "DriverProfile",
                driverProfile.Id);

            return Ok(new
            {
                driverProfile.Id,
                driverProfile.VerificationStatus
            });
        }

        [HttpPost("drivers/{id}/reject")]
        public async Task<IActionResult> RejectDriver(Guid id, RejectRequestDto dto)
        {
            var adminId = GetCurrentUserId();
            var driverProfile = await _dbcontext.DriverProfiles.FirstOrDefaultAsync(d => d.Id == id);
            if (driverProfile == null)
                return NotFound("بروفايل السائق غير موجود");

            driverProfile.VerificationStatus = VerificationStatus.Rejected;
            driverProfile.RejectionReason = dto.Reason;
            driverProfile.IsAvailable = false;

            await _dbcontext.SaveChangesAsync();
            await _auditService.LogAsync(adminId, "RejectDriver", "DriverProfile", driverProfile.Id,
                    $"تم رفض السائق — السبب: {dto.Reason}");
            await _notificationService.NotifyAsync(
                driverProfile.UserId,
                "تم رفض طلب التوثيق",
                $"تم رفض توثيق حسابك كسائق. السبب: {dto.Reason}",
                "DriverRejected",
                "DriverProfile",
                driverProfile.Id);

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
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    c.Id,
                    CreatedByName = c.CreatedByUser.FullName,
                    OrderNumber = c.Order != null ? c.Order.OrderNumber : null,
                    TargetName = c.Merchant != null ? c.Merchant.Name
                               : c.DriverProfile != null ? c.DriverProfile.User.FullName
                               : c.Offer != null ? c.Offer.Title
                               : null,
                    c.Category,
                    c.Description,
                    c.EvidenceUrl,
                    c.Status,
                    c.AdminResponse,
                    c.CreatedAt,
                    c.ResolvedAt
                })
                .ToListAsync();

            return Ok(complaints);
        }
        [HttpPut("complaints/{id}/resolve")]
        public async Task<IActionResult> ResolveComplaint(Guid id, ResolveComplaintRequestDto dto)
        {
            var adminId = GetCurrentUserId();
            var now = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(dto.AdminResponse))
                return BadRequest("الرد على الشكوى مطلوب");

            if (dto.Status != ComplaintStatus.Resolved
                && dto.Status != ComplaintStatus.Rejected
                && dto.Status != ComplaintStatus.UnderReview)
                return BadRequest("حالة غير صالحة");

            var complaint = await _dbcontext.Complaints.FirstOrDefaultAsync(c => c.Id == id);
            if (complaint == null)
                return NotFound("الشكوى غير موجودة");

            if (complaint.Status == ComplaintStatus.Resolved || complaint.Status == ComplaintStatus.Rejected)
                return BadRequest("الشكوى مغلقة بالفعل");

            complaint.Status = dto.Status;
            complaint.AdminResponse = dto.AdminResponse.Trim();

            if (dto.Status == ComplaintStatus.Resolved || dto.Status == ComplaintStatus.Rejected)
            {
                complaint.ResolvedByUserId = adminId;
                complaint.ResolvedAt = now;
            }

            await _dbcontext.SaveChangesAsync();
            await _auditService.LogAsync(adminId, "ResolveComplaint", "Complaint", complaint.Id,
                $"تم حل الشكوى — الرد: {dto.AdminResponse}");
            var title = dto.Status switch
            {
                ComplaintStatus.Resolved => "تم حل شكواك",
                ComplaintStatus.Rejected => "تم إغلاق شكواك",
                _ => "شكواك قيد المراجعة"
            };

            await _notificationService.NotifyAsync(
                complaint.CreatedByUserId,
                title,
                dto.AdminResponse,
                "ComplaintUpdated",
                "Complaint",
                complaint.Id);

            return Ok(new
            {
                complaint.Id,
                complaint.Status,
                complaint.AdminResponse,
                complaint.ResolvedAt
            });
        }
        private Guid GetCurrentUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
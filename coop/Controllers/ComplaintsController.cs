using coop.Dtos.ComplaintsController;
using coop.Enums;
using coop.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace coop.Controllers
{
    [Route("api/complaints")]
    [ApiController]
    [Authorize]
    public class ComplaintsController : ControllerBase
    {
        private readonly CoopDbContext _dbcontext;

        public ComplaintsController(CoopDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }

        [HttpPost]
        public async Task<IActionResult> CreateComplaint(CreateComplaintRequest dto)
        {
            var userId = GetCurrentUserId();
            var now = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(dto.Category))
                return BadRequest("تصنيف الشكوى مطلوب");

            if (string.IsNullOrWhiteSpace(dto.Description))
                return BadRequest("وصف الشكوى مطلوب");

            if (dto.OrderId == null && dto.MerchantId == null
                && dto.DriverProfileId == null && dto.OfferId == null)
                return BadRequest("يجب تحديد الطلب أو التاجر أو السائق أو العرض المشتكى عليه");

            string? orderNumber = null;
            string? targetName = null;

            if (dto.OrderId != null)
            {
                var order = await _dbcontext.Orders
                    .Where(o => o.Id == dto.OrderId
                             && (o.CustomerUserId == userId || o.Merchant.OwnerUserId == userId))
                    .Select(o => new { o.OrderNumber })
                    .FirstOrDefaultAsync();

                if (order == null)
                    return BadRequest("الطلب غير موجود أو لا علاقة لك به");

                orderNumber = order.OrderNumber;
            }

            if (dto.MerchantId != null)
            {
                targetName = await _dbcontext.Merchants
                    .Where(m => m.Id == dto.MerchantId)
                    .Select(m => m.Name)
                    .FirstOrDefaultAsync();

                if (targetName == null)
                    return BadRequest("التاجر غير موجود");
            }

            if (dto.DriverProfileId != null)
            {
                targetName = await _dbcontext.DriverProfiles
                    .Where(d => d.Id == dto.DriverProfileId)
                    .Select(d => d.User.FullName)
                    .FirstOrDefaultAsync();

                if (targetName == null)
                    return BadRequest("السائق غير موجود");
            }

            if (dto.OfferId != null)
            {
                targetName = await _dbcontext.Offers
                    .Where(o => o.Id == dto.OfferId)
                    .Select(o => o.Title)
                    .FirstOrDefaultAsync();

                if (targetName == null)
                    return BadRequest("العرض غير موجود");
            }

            var complaint = new Complaint
            {
                Id = Guid.NewGuid(),
                CreatedByUserId = userId,
                OrderId = dto.OrderId,
                MerchantId = dto.MerchantId,
                DriverProfileId = dto.DriverProfileId,
                OfferId = dto.OfferId,
                Category = dto.Category.Trim(),
                Description = dto.Description.Trim(),
                EvidenceUrl = dto.EvidenceUrl,
                Status = ComplaintStatus.Open,
                CreatedAt = now
            };

            _dbcontext.Complaints.Add(complaint);
            await _dbcontext.SaveChangesAsync();

            return StatusCode(201, new ComplaintResponse
            {
                Id = complaint.Id,
                OrderNumber = orderNumber,
                TargetName = targetName,
                Category = complaint.Category,
                Description = complaint.Description,
                EvidenceUrl = complaint.EvidenceUrl,
                Status = complaint.Status,
                AdminResponse = complaint.AdminResponse,
                CreatedAt = complaint.CreatedAt,
                ResolvedAt = complaint.ResolvedAt
            });
        }
        [HttpGet("my")]
        public async Task<IActionResult> GetMyComplaints([FromQuery] ComplaintStatus? status)
        {
            var userId = GetCurrentUserId();

            var query = _dbcontext.Complaints
                .Where(c => c.CreatedByUserId == userId);

            if (status != null)
                query = query.Where(c => c.Status == status);

            var complaints = await query
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new ComplaintResponse
                {
                    Id = c.Id,
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
        [HttpGet("{id}")]
        public async Task<IActionResult> GetComplaintById(Guid id)
        {
            var userId = GetCurrentUserId();

            var complaint = await _dbcontext.Complaints
                .Where(c => c.Id == id && c.CreatedByUserId == userId)
                .Select(c => new ComplaintResponse
                {
                    Id = c.Id,
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
                .FirstOrDefaultAsync();

            if (complaint == null)
                return NotFound("الشكوى غير موجودة");

            return Ok(complaint);
        }

        private Guid GetCurrentUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
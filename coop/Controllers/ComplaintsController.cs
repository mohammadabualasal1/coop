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

            if (dto.OrderId != null)
            {
                var isRelatedToOrder = await _dbcontext.Orders
                    .AnyAsync(o => o.Id == dto.OrderId
                                && (o.CustomerUserId == userId
                                 || o.Merchant.OwnerUserId == userId));

                if (!isRelatedToOrder)
                    return BadRequest("الطلب غير موجود أو لا علاقة لك به");
            }

            if (dto.MerchantId != null)
            {
                var merchantExists = await _dbcontext.Merchants.AnyAsync(m => m.Id == dto.MerchantId);
                if (!merchantExists)
                    return BadRequest("التاجر غير موجود");
            }

            if (dto.DriverProfileId != null)
            {
                var driverExists = await _dbcontext.DriverProfiles.AnyAsync(d => d.Id == dto.DriverProfileId);
                if (!driverExists)
                    return BadRequest("السائق غير موجود");
            }

            if (dto.OfferId != null)
            {
                var offerExists = await _dbcontext.Offers.AnyAsync(o => o.Id == dto.OfferId);
                if (!offerExists)
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
                Category = complaint.Category,
                Description = complaint.Description,
                EvidenceUrl = complaint.EvidenceUrl,
                Status = complaint.Status,
                AdminResponse = complaint.AdminResponse,
                CreatedAt = complaint.CreatedAt,
                ResolvedAt = complaint.ResolvedAt
            });
        }

        private Guid GetCurrentUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
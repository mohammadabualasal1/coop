using coop.Dtos.MerchantBranchesController;
using coop.Dtos.MerchantsController;
using coop.Enums;
using coop.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Claims;

namespace coop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Merchant")]
    public class MerchantsController : ControllerBase
    {
        private readonly CoopDbContext _dbcontext;

        public MerchantsController(CoopDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }
        [HttpPost("AddMerchant")]
        public async Task<IActionResult> AddMerchant([FromBody] CreateMerchantRequestDto dto)
        {
            var userId = GetCurrentUserId();
            var existingMerchant = await _dbcontext.Merchants.AnyAsync(m => m.OwnerUserId == userId);
            if (existingMerchant)
            {
                return BadRequest("لديك بروفايل تاجر بالفعل");
            }
            var newMerchant = new Merchant
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description,
                OwnerUserId = userId,
                CreatedAt = DateTime.UtcNow,
                ContactEmail = dto.ContactEmail,
                ContactPhone = dto.ContactPhone,
                CoverImageUrl = dto.CoverImageUrl,
                LogoUrl = dto.LogoUrl,
                VerificationStatus = VerificationStatus.Pending,
                IsActive = true,
                RegistrationNumber = dto.RegistrationNumber,

            };
            _dbcontext.Merchants.Add(newMerchant);
            await _dbcontext.SaveChangesAsync();
            return Ok(newMerchant);
        }
        [HttpGet("my")]
        public async Task<IActionResult> GetMyMerchant()
        {
            var userId = GetCurrentUserId();
            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
            if (merchant == null)
                return NotFound("لا يوجد بروفايل تاجر مرتبط بحسابك");
            return Ok(merchant);
        }
        [HttpPut("my")]
        public async Task<IActionResult> UpdateMyMerchant([FromBody] UpdateMerchantRequestDto dto)
        {
            var userId = GetCurrentUserId();
          var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
            if (merchant == null)
                return NotFound("لا يوجد بروفايل تاجر مرتبط بحسابك");


            merchant.Name = dto.Name;
            merchant.Description = dto.Description;
            merchant.ContactEmail = dto.ContactEmail;
            merchant.ContactPhone = dto.ContactPhone;
            merchant.CoverImageUrl = dto.CoverImageUrl;
            merchant.LogoUrl = dto.LogoUrl;
            await _dbcontext.SaveChangesAsync();
            return Ok(merchant);

        }

        [HttpPost("my/submit-verification")]
        public async Task<IActionResult> SubmitVerification()
        {
            var userId = GetCurrentUserId();
            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
            if (merchant == null)
                return NotFound("لا يوجد بروفايل تاجر مرتبط بحسابك");

            if (merchant.VerificationStatus != VerificationStatus.Rejected &&
                merchant.VerificationStatus != VerificationStatus.NeedsInformation)
                return BadRequest("طلبك أصلاً قيد المراجعة أو تمت الموافقة عليه");

            var hasDocuments = await _dbcontext.VerificationDocuments.AnyAsync(d => d.MerchantId == merchant.Id);
            if (!hasDocuments)
                return BadRequest("لازم ترفع وثيقة تحقق واحدة على الأقل");
            merchant.VerificationStatus = VerificationStatus.Pending;
            merchant.RejectionReason = null;
            await _dbcontext.SaveChangesAsync();
            return Ok(merchant);
        }



        [HttpGet("my/verification-status")]
        public async Task<IActionResult> GetVerificationStatus()
        {
           var userId = GetCurrentUserId();
            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
            if (merchant == null)
                return NotFound("لا يوجد بروفايل تاجر مرتبط بحسابك");
            return Ok(new VerificationStatusResponseDto
            {
                VerificationStatus = merchant.VerificationStatus,
                RejectionReason = merchant.RejectionReason,
                VerifiedAt = merchant.VerifiedAt
            });
        }

        [HttpPost]
        public async Task<IActionResult> AddBranch([FromBody] CreateBranchRequestDto dto)
        {
            var userId = GetCurrentUserId();
            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
            if (merchant == null)
                return NotFound("لا يوجد بروفايل تاجر مرتبط بحسابك");

            if (merchant.VerificationStatus != VerificationStatus.Approved)
                return StatusCode(403, "يجب ان يتم توثيق حسابك قبل إضافة فروع");
            var isFirstBranch = !await _dbcontext.MerchantBranches.AnyAsync(b => b.MerchantId == merchant.Id);

            var newBranch = new MerchantBranch
            {
                Id = Guid.NewGuid(),
                MerchantId = merchant.Id,
                Name = dto.Name,
                Address = dto.Address,
                CreatedAt = DateTime.UtcNow,
                City= dto.City,
                Area = dto.Area,
                Latitude= dto.Latitude,
                Longitude= dto.Longitude,
                OpeningTime = dto.OpeningTime,
                ClosingTime = dto.ClosingTime,
                DeliveryRadiusKm = dto.DeliveryRadiusKm,
                MinimumOrderAmount=dto.MinimumOrderAmount,
                BaseDeliveryFee=dto.BaseDeliveryFee,
            };
            _dbcontext.MerchantBranches.Add(newBranch);
            await _dbcontext.SaveChangesAsync();
            return Ok(newBranch);
        }

        [HttpGet]
        public async Task<IActionResult> GetMyBranches()
        {
            var userId = GetCurrentUserId();
            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
            if (merchant == null)
                return NotFound("لا يوجد بروفايل تاجر مرتبط بحسابك");

            var branches = await _dbcontext.MerchantBranches
                .Where(b => b.MerchantId == merchant.Id && b.IsActive)
                .ToListAsync();

            return Ok(branches);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBranchById(Guid id)
        {
            var userId = GetCurrentUserId();
            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
            if (merchant == null)
                return NotFound("لا يوجد بروفايل تاجر مرتبط بحسابك");
            var branch = await _dbcontext.MerchantBranches.Where(b => b.MerchantId == merchant.Id && b.Id == id).FirstOrDefaultAsync();
            if (branch == null)
                return NotFound("الفرع غير موجود");
            return Ok(branch);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBranch(Guid id, [FromBody] UpdateBranchRequestDto dto)
        {
            var userId = GetCurrentUserId();
            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
            if (merchant == null)
                return NotFound("لا يوجد بروفايل تاجر مرتبط بحسابك");

            var branch = await _dbcontext.MerchantBranches.FirstOrDefaultAsync(b => b.Id == id && b.MerchantId == merchant.Id);
            if (branch == null)
                return NotFound("الفرع غير موجود");

            branch.Name = dto.Name;
            branch.Address = dto.Address;
            branch.City = dto.City;
            branch.Area = dto.Area;
            branch.Latitude = dto.Latitude;
            branch.Longitude = dto.Longitude;
            branch.PhoneNumber = dto.PhoneNumber;
            branch.OpeningTime = dto.OpeningTime;
            branch.ClosingTime = dto.ClosingTime;
            branch.DeliveryRadiusKm = dto.DeliveryRadiusKm;
            branch.MinimumOrderAmount = dto.MinimumOrderAmount;
            branch.BaseDeliveryFee = dto.BaseDeliveryFee;

            await _dbcontext.SaveChangesAsync();
            return Ok(branch);
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeactivateBranch(Guid id)
        {
            var userId = GetCurrentUserId();
            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
            if (merchant == null)
                return NotFound("لا يوجد بروفايل تاجر مرتبط بحسابك");

            var branch = await _dbcontext.MerchantBranches.FirstOrDefaultAsync(b => b.Id == id && b.MerchantId == merchant.Id);
            if (branch == null)
                return NotFound("الفرع غير موجود");

            if (branch.IsMainBranch)
                return BadRequest("لا يمكن تعطيل الفرع الرئيسي، عيّن فرع رئيسي آخر أولاً");

            branch.IsActive = false;
            await _dbcontext.SaveChangesAsync();
            return NoContent();
        }
        [HttpPut("{id}/set-main")]
        public async Task<IActionResult> SetMainBranch(Guid id)
        {
            var userId = GetCurrentUserId();
            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
            if (merchant == null)
                return NotFound("لا يوجد بروفايل تاجر مرتبط بحسابك");

            var branch = await _dbcontext.MerchantBranches.FirstOrDefaultAsync(b => b.Id == id && b.MerchantId == merchant.Id);
            if (branch == null)
                return NotFound("الفرع غير موجود");

            if (!branch.IsActive)
                return BadRequest("لا يمكن تعيين فرع معطّل كفرع رئيسي");

            var allBranches = await _dbcontext.MerchantBranches
                .Where(b => b.MerchantId == merchant.Id)
                .ToListAsync();

            foreach (var b in allBranches)
                b.IsMainBranch = (b.Id == id);

            await _dbcontext.SaveChangesAsync();
            return Ok(branch);
        }

        private Guid GetCurrentUserId() =>
          Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}

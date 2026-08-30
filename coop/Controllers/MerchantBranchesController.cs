using coop.Dtos.MerchantBranchesController;
using coop.Enums;
using coop.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using NetTopologySuite.Geometries;

namespace coop.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = "Merchant")]
    [ApiController]
    public class MerchantBranchesController : ControllerBase
    {

        private readonly CoopDbContext _dbcontext;

        public MerchantBranchesController(CoopDbContext dbcontext)
        {
            _dbcontext = dbcontext;
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
                City = dto.City,
                Area = dto.Area,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Location = new Point(dto.Longitude, dto.Latitude) { SRID = 4326 },
                PhoneNumber = dto.PhoneNumber,
                OpeningTime = dto.OpeningTime,
                ClosingTime = dto.ClosingTime,
                DeliveryRadiusKm = dto.DeliveryRadiusKm,
                MinimumOrderAmount = dto.MinimumOrderAmount,
                BaseDeliveryFee = dto.BaseDeliveryFee,
                IsMainBranch = isFirstBranch,
                IsActive = true,
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
            branch.Location = new Point(dto.Longitude, dto.Latitude) { SRID = 4326 };
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

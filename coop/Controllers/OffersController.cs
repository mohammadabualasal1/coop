using coop.Dtos.OffersController;
using coop.Enums;
using coop.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace coop.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = "Merchant")]
    [ApiController]
    public class OffersController : ControllerBase
    {
        private readonly CoopDbContext _dbcontext;

        public OffersController(CoopDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }

        [HttpPost]
        public async Task<IActionResult> AddOffer([FromBody] CreateOfferRequest dto)
        {
            var userId = GetCurrentUserId();
            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
            if (merchant == null)
                return NotFound("لا يوجد بروفايل تاجر مرتبط بحسابك");

            if (merchant.VerificationStatus != VerificationStatus.Approved)
                return StatusCode(403, "يجب ان يتم توثيق حسابك قبل إنشاء عروض");

            var product = await _dbcontext.Products.FirstOrDefaultAsync(p => p.Id == dto.ProductId && p.MerchantId == merchant.Id);
            if (product == null)
                return NotFound("المنتج غير موجود");

            if (dto.DiscountedPrice <= 0 || dto.DiscountedPrice >= dto.OriginalPrice)
                return BadRequest("السعر بعد الخصم يجب أن يكون أكبر من صفر وأقل من السعر الأصلي");

            if (dto.StartAt >= dto.EndAt)
                return BadRequest("تاريخ البداية يجب أن يكون قبل تاريخ النهاية");

            if (dto.EndAt <= DateTime.UtcNow)
                return BadRequest("تاريخ النهاية يجب أن يكون في المستقبل");

            var newOffer = new Offer
            {
                Id = Guid.NewGuid(),
                ProductId = dto.ProductId,
                MerchantId = merchant.Id,
                Title = dto.Title,
                Description = dto.Description,
                OriginalPrice = dto.OriginalPrice,
                DiscountedPrice = dto.DiscountedPrice,
                DiscountPercentage = Math.Round((dto.OriginalPrice - dto.DiscountedPrice) / dto.OriginalPrice * 100, 2),
                StartAt = dto.StartAt,
                EndAt = dto.EndAt,
                Status = OfferStatus.Draft,
                MaximumQuantityPerCustomer = dto.MaximumQuantityPerCustomer,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            _dbcontext.Offers.Add(newOffer);
            await _dbcontext.SaveChangesAsync();
            return Ok(newOffer);
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyOffers()
        {
            var userId = GetCurrentUserId();
            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
            if (merchant == null)
                return NotFound("لا يوجد بروفايل تاجر مرتبط بحسابك");

            var offers = await _dbcontext.Offers
                .Where(o => o.MerchantId == merchant.Id)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return Ok(offers);
        }



        [HttpGet("{id}")]
        public async Task<IActionResult> GetOfferById(Guid id)
        {
            var userId = GetCurrentUserId();
            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
            if (merchant == null)
                return NotFound("لا يوجد بروفايل تاجر مرتبط بحسابك");

            var offer = await _dbcontext.Offers.FirstOrDefaultAsync(o => o.Id == id && o.MerchantId == merchant.Id);
            if (offer == null)
                return NotFound("العرض غير موجود");

            var branchOffers = await _dbcontext.BranchOffers
                .Where(bo => bo.OfferId == id)
                .Select(bo => new BranchOfferResponse
                {
                    Id = bo.Id,
                    MerchantBranchId = bo.MerchantBranchId,
                    TotalStock = bo.TotalStock,
                    ReservedStock = bo.ReservedStock,
                    SoldStock = bo.SoldStock,
                    IsAvailable = bo.IsAvailable
                })
                .ToListAsync();

            return Ok(new { offer, branches = branchOffers });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOffer(Guid id, [FromBody] UpdateOfferRequestDto dto)
        {
            var userId = GetCurrentUserId();
            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
            if (merchant == null)
                return NotFound("لا يوجد بروفايل تاجر مرتبط بحسابك");

            var offer = await _dbcontext.Offers.FirstOrDefaultAsync(o => o.Id == id && o.MerchantId == merchant.Id);
            if (offer == null)
                return NotFound("العرض غير موجود");

            if (offer.Status != OfferStatus.Draft && offer.Status != OfferStatus.Rejected)
                return BadRequest("لا يمكن تعديل العرض بعد تقديمه للمراجعة");

            if (dto.DiscountedPrice <= 0 || dto.DiscountedPrice >= dto.OriginalPrice)
                return BadRequest("السعر بعد الخصم يجب أن يكون أكبر من صفر وأقل من السعر الأصلي");

            if (dto.StartAt >= dto.EndAt)
                return BadRequest("تاريخ البداية يجب أن يكون قبل تاريخ النهاية");

            offer.Title = dto.Title;
            offer.Description = dto.Description;
            offer.OriginalPrice = dto.OriginalPrice;
            offer.DiscountedPrice = dto.DiscountedPrice;
            offer.DiscountPercentage = Math.Round((dto.OriginalPrice - dto.DiscountedPrice) / dto.OriginalPrice * 100, 2);
            offer.StartAt = dto.StartAt;
            offer.EndAt = dto.EndAt;
            offer.MaximumQuantityPerCustomer = dto.MaximumQuantityPerCustomer;
            offer.UpdatedAt = DateTime.UtcNow;

            await _dbcontext.SaveChangesAsync();
            return Ok(offer);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOffer(Guid id)
        {
            var userId = GetCurrentUserId();
            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
            if (merchant == null)
                return NotFound("لا يوجد بروفايل تاجر مرتبط بحسابك");

            var offer = await _dbcontext.Offers.FirstOrDefaultAsync(o => o.Id == id && o.MerchantId == merchant.Id);
            if (offer == null)
                return NotFound("العرض غير موجود");

            if (offer.Status != OfferStatus.Draft)
                return BadRequest("لا يمكن حذف العرض إلا وهو مسودة، استخدم الإلغاء بدلاً من ذلك");

            var branchOffers = await _dbcontext.BranchOffers.Where(bo => bo.OfferId == id).ToListAsync();
            _dbcontext.BranchOffers.RemoveRange(branchOffers);
            _dbcontext.Offers.Remove(offer);

            await _dbcontext.SaveChangesAsync();
            return NoContent();
        }
        [HttpPost("{id}/branches")]
        public async Task<IActionResult> AddBranchStock(Guid id, [FromBody] AddBranchStockRequestDto dto)
        {
            var userId = GetCurrentUserId();
            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
            if (merchant == null)
                return NotFound("لا يوجد بروفايل تاجر مرتبط بحسابك");

            var offer = await _dbcontext.Offers.FirstOrDefaultAsync(o => o.Id == id && o.MerchantId == merchant.Id);
            if (offer == null)
                return NotFound("العرض غير موجود");

            var branch = await _dbcontext.MerchantBranches
                .FirstOrDefaultAsync(b => b.Id == dto.MerchantBranchId && b.MerchantId == merchant.Id && b.IsActive);
            if (branch == null)
                return NotFound("الفرع غير موجود");

            var alreadyAdded = await _dbcontext.BranchOffers
                .AnyAsync(bo => bo.OfferId == id && bo.MerchantBranchId == dto.MerchantBranchId);
            if (alreadyAdded)
                return Conflict("هذا الفرع مضاف للعرض بالفعل");

            if (dto.TotalStock <= 0)
                return BadRequest("الكمية يجب أن تكون أكبر من صفر");

            var branchOffer = new BranchOffer
            {
                Id = Guid.NewGuid(),
                OfferId = id,
                MerchantBranchId = dto.MerchantBranchId,
                TotalStock = dto.TotalStock,
                ReservedStock = 0,
                SoldStock = 0,
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow
            };

            _dbcontext.BranchOffers.Add(branchOffer);
            await _dbcontext.SaveChangesAsync();

            return Ok(new BranchOfferResponse
            {
                Id = branchOffer.Id,
                MerchantBranchId = branchOffer.MerchantBranchId,
                TotalStock = branchOffer.TotalStock,
                ReservedStock = branchOffer.ReservedStock,
                SoldStock = branchOffer.SoldStock,
                IsAvailable = branchOffer.IsAvailable
            });
        }

        [HttpPut("{id}/branches/{branchOfferId}")]
        public async Task<IActionResult> UpdateBranchStock(Guid id, Guid branchOfferId, [FromBody] UpdateBranchStockRequest dto)
        {
            var userId = GetCurrentUserId();
            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
            if (merchant == null)
                return NotFound("لا يوجد بروفايل تاجر مرتبط بحسابك");

            var offer = await _dbcontext.Offers.FirstOrDefaultAsync(o => o.Id == id && o.MerchantId == merchant.Id);
            if (offer == null)
                return NotFound("العرض غير موجود");

            var branchOffer = await _dbcontext.BranchOffers.FirstOrDefaultAsync(bo => bo.Id == branchOfferId && bo.OfferId == id);
            if (branchOffer == null)
                return NotFound("الفرع غير مضاف لهذا العرض");

            if (dto.TotalStock < branchOffer.ReservedStock + branchOffer.SoldStock)
                return BadRequest("الكمية الجديدة أقل من الكمية المحجوزة والمباعة");

            branchOffer.TotalStock = dto.TotalStock;
            branchOffer.IsAvailable = dto.IsAvailable;

            await _dbcontext.SaveChangesAsync();

            return Ok(new BranchOfferResponse
            {
                Id = branchOffer.Id,
                MerchantBranchId = branchOffer.MerchantBranchId,
                TotalStock = branchOffer.TotalStock,
                ReservedStock = branchOffer.ReservedStock,
                SoldStock = branchOffer.SoldStock,
                IsAvailable = branchOffer.IsAvailable
            });
        }

        private Guid GetCurrentUserId() =>
          Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}

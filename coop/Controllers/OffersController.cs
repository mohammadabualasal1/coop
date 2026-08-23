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




        private Guid GetCurrentUserId() =>
          Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}

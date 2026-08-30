using System.Security.Claims;
using coop.Dtos.CheckoutController;
using coop.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace coop.Controllers
{
    [ApiController]
    [Route("api/checkout")]
    [Authorize(Roles = "Customer")]
    public class CheckoutController : ControllerBase
    {
        private CoopDbContext _dbcontext;
        private const double FreeRadiusKm = 3;
        private const decimal PerKmFee = 0.25m;
        public CheckoutController(CoopDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }

        [HttpPost("calculate")]
        public async Task<IActionResult> Calculate(CheckoutSummaryRequestDto dto)
        {
            var userId = GetCurrentUserId();
            var now = DateTime.UtcNow;

            var addressExists = await _dbcontext.CustomerAddresses
                .AnyAsync(a => a.Id == dto.CustomerAddressId && a.CustomerUserId == userId);

            if (!addressExists)
                return NotFound("العنوان غير موجود");

            var cart = await _dbcontext.Carts
                .FirstOrDefaultAsync(c => c.CustomerUserId == userId);

            if (cart == null)
                return BadRequest("السلة فارغة");

            var items = await _dbcontext.CartItems
                .Where(ci => ci.CartId == cart.Id)
                .Select(ci => new
                {
                    ci.Quantity,
                    ci.Offer.Title,
                    ci.Offer.OriginalPrice,
                    ci.Offer.DiscountedPrice,
                    ci.Offer.Status,
                    ci.Offer.StartAt,
                    ci.Offer.EndAt
                })
                .ToListAsync();

            if (items.Count == 0)
                return BadRequest("السلة فارغة");

            var invalidOffer = items.FirstOrDefault(i => i.Status != OfferStatus.Active
                                                     || i.StartAt > now
                                                     || i.EndAt < now);

            if (invalidOffer != null)
                return BadRequest($"العرض \"{invalidOffer.Title}\" لم يعد متاحاً، الرجاء تحديث السلة");

            var branch = await _dbcontext.MerchantBranches
                .FirstOrDefaultAsync(b => b.Id == cart.MerchantBranchId && b.IsActive);

            if (branch == null)
                return BadRequest("الفرع غير متاح حالياً");

            var subtotal = items.Sum(i => i.OriginalPrice * i.Quantity);
            var totalDiscount = items.Sum(i => (i.OriginalPrice - i.DiscountedPrice) * i.Quantity);
            var itemsTotal = subtotal - totalDiscount;

            if (itemsTotal < branch.MinimumOrderAmount)
                return BadRequest($"الحد الأدنى للطلب من هذا الفرع هو {branch.MinimumOrderAmount}");

            var address = await _dbcontext.CustomerAddresses
                .FirstOrDefaultAsync(a => a.Id == dto.CustomerAddressId);

            var deliveryFee = branch.BaseDeliveryFee;

            if (branch.Location != null && address?.Location != null)
            {
                var distanceMeters = await _dbcontext.MerchantBranches
                    .Where(b => b.Id == branch.Id)
                    .Select(b => b.Location!.Distance(address.Location))
                    .FirstAsync();

                var distanceKm = distanceMeters / 1000;

                if (distanceKm > FreeRadiusKm)
                    deliveryFee += (decimal)Math.Ceiling(distanceKm - FreeRadiusKm) * PerKmFee;
            }
            return Ok(new CheckoutSummaryResponseDto
            {
                Subtotal = subtotal,
                TotalDiscount = totalDiscount,
                DeliveryFee = deliveryFee,
                TotalAmount = itemsTotal + deliveryFee
            });
        }

        private Guid GetCurrentUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
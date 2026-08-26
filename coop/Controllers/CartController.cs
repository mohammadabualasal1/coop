using System.Security.Claims;
using coop.Dtos.CartController;
using coop.Enums;
using coop.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace coop.Controllers
{
    [ApiController]
    [Route("api/cart")]
    [Authorize(Roles = "Customer")]
    public class CartController : ControllerBase
    {
        private readonly CoopDbContext _dbcontext;

        public CartController(CoopDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyCart()
        {
            var userId = GetCurrentUserId();

            var cart = await _dbcontext.Carts
                .FirstOrDefaultAsync(c => c.CustomerUserId == userId);

            if (cart == null)
            {
                return Ok(new CartResponseDto
                {
                    Id = Guid.Empty,
                    MerchantBranchId = Guid.Empty,
                    Items = new List<CartItemResponse>(),
                    Subtotal = 0,
                    TotalDiscount = 0,
                    EstimatedTotal = 0
                });
            }

            var items = await _dbcontext.CartItems
                .Where(ci => ci.CartId == cart.Id)
                .OrderBy(ci => ci.CreatedAt)
                .Select(ci => new CartItemResponse
                {
                    Id = ci.Id,
                    OfferId = ci.OfferId,
                    Title = ci.Offer.Title,
                    Quantity = ci.Quantity,
                    UnitPrice = ci.Offer.DiscountedPrice,
                    LineTotal = ci.Offer.DiscountedPrice * ci.Quantity
                })
                .ToListAsync();

            var subtotal = await _dbcontext.CartItems
                .Where(ci => ci.CartId == cart.Id)
                .SumAsync(ci => ci.Offer.OriginalPrice * ci.Quantity);

            var totalDiscount = await _dbcontext.CartItems
                .Where(ci => ci.CartId == cart.Id)
                .SumAsync(ci => (ci.Offer.OriginalPrice - ci.Offer.DiscountedPrice) * ci.Quantity);

            return Ok(new CartResponseDto
            {
                Id = cart.Id,
                MerchantBranchId = cart.MerchantBranchId,
                Items = items,
                Subtotal = subtotal,
                TotalDiscount = totalDiscount,
                EstimatedTotal = subtotal - totalDiscount
            });
        }

        [HttpPost("items")]
        public async Task<IActionResult> AddItem(AddCartItemRequestDto dto)
        {
            var userId = GetCurrentUserId();
            var now = DateTime.UtcNow;

            if (dto.Quantity < 1)
                return BadRequest("الكمية يجب أن تكون 1 أو أكثر");

            var offer = await _dbcontext.Offers.FirstOrDefaultAsync(o => o.Id == dto.OfferId);
            if (offer == null)
                return NotFound("العرض غير موجود");

            if (offer.Status != OfferStatus.Active || offer.StartAt > now || offer.EndAt < now)
                return BadRequest("العرض غير متاح حالياً");

            var cart = await _dbcontext.Carts.FirstOrDefaultAsync(c => c.CustomerUserId == userId);

            var branchOfferQuery = _dbcontext.BranchOffers
                .Where(bo => bo.OfferId == offer.Id
                          && bo.IsAvailable
                          && bo.MerchantBranch.IsActive);

            if (cart != null)
                branchOfferQuery = branchOfferQuery.Where(bo => bo.MerchantBranchId == cart.MerchantBranchId);

            var branchOffer = await branchOfferQuery
                .OrderByDescending(bo => bo.TotalStock - bo.ReservedStock - bo.SoldStock)
                .FirstOrDefaultAsync();

            if (branchOffer == null)
            {
                if (cart != null)
                    return BadRequest("لا يمكن إضافة عروض من فرع آخر، أفرغ السلة أولاً");

                return BadRequest("العرض غير متاح في أي فرع حالياً");
            }

            var availableStock = branchOffer.TotalStock - branchOffer.ReservedStock - branchOffer.SoldStock;

            if (availableStock < 1)
                return BadRequest("نفد مخزون هذا العرض");

            if (cart == null)
            {
                cart = new Cart
                {
                    Id = Guid.NewGuid(),
                    CustomerUserId = userId,
                    MerchantBranchId = branchOffer.MerchantBranchId,
                    CreatedAt = now,
                    UpdatedAt = now,
                    ExpiresAt = now.AddHours(24)
                };

                _dbcontext.Carts.Add(cart);
            }

            var existingItem = await _dbcontext.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.OfferId == offer.Id);

            var newQuantity = (existingItem?.Quantity ?? 0) + dto.Quantity;

            if (newQuantity > availableStock)
                return BadRequest($"الكمية المتاحة من هذا العرض هي {availableStock} فقط");

            if (offer.MaximumQuantityPerCustomer != null && newQuantity > offer.MaximumQuantityPerCustomer)
                return BadRequest($"الحد الأقصى لهذا العرض هو {offer.MaximumQuantityPerCustomer} لكل زبون");

            if (existingItem != null)
            {
                existingItem.Quantity = newQuantity;
                existingItem.UpdatedAt = now;
            }
            else
            {
                _dbcontext.CartItems.Add(new CartItem
                {
                    Id = Guid.NewGuid(),
                    CartId = cart.Id,
                    OfferId = offer.Id,
                    Quantity = dto.Quantity,
                    AddedUnitPrice = offer.DiscountedPrice,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            cart.UpdatedAt = now;
            cart.ExpiresAt = now.AddHours(24);

            await _dbcontext.SaveChangesAsync();

            var responseItems = await _dbcontext.CartItems
                .Where(ci => ci.CartId == cart.Id)
                .OrderBy(ci => ci.CreatedAt)
                .Select(ci => new CartItemResponse
                {
                    Id = ci.Id,
                    OfferId = ci.OfferId,
                    Title = ci.Offer.Title,
                    Quantity = ci.Quantity,
                    UnitPrice = ci.Offer.DiscountedPrice,
                    LineTotal = ci.Offer.DiscountedPrice * ci.Quantity
                })
                .ToListAsync();

            var responseSubtotal = await _dbcontext.CartItems
                .Where(ci => ci.CartId == cart.Id)
                .SumAsync(ci => ci.Offer.OriginalPrice * ci.Quantity);

            var responseTotalDiscount = await _dbcontext.CartItems
                .Where(ci => ci.CartId == cart.Id)
                .SumAsync(ci => (ci.Offer.OriginalPrice - ci.Offer.DiscountedPrice) * ci.Quantity);

            return Ok(new CartResponseDto
            {
                Id = cart.Id,
                MerchantBranchId = cart.MerchantBranchId,
                Items = responseItems,
                Subtotal = responseSubtotal,
                TotalDiscount = responseTotalDiscount,
                EstimatedTotal = responseSubtotal - responseTotalDiscount
            });
        }

        private Guid GetCurrentUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
using System.Security.Claims;
using coop.Dtos.CartController;
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
                .Select(ci => new
                {
                    ci.Id,
                    ci.OfferId,
                    ci.Quantity,
                    ci.Offer.Title,
                    ci.Offer.OriginalPrice,
                    ci.Offer.DiscountedPrice
                })
                .ToListAsync();

            var subtotal = items.Sum(i => i.OriginalPrice * i.Quantity);
            var totalDiscount = items.Sum(i => (i.OriginalPrice - i.DiscountedPrice) * i.Quantity);

            return Ok(new CartResponseDto
            {
                Id = cart.Id,
                MerchantBranchId = cart.MerchantBranchId,
                Items = items.Select(i => new CartItemResponse
                {
                    Id = i.Id,
                    OfferId = i.OfferId,
                    Title = i.Title,
                    Quantity = i.Quantity,
                    UnitPrice = i.DiscountedPrice,
                    LineTotal = i.DiscountedPrice * i.Quantity
                }).ToList(),
                Subtotal = subtotal,
                TotalDiscount = totalDiscount,
                EstimatedTotal = subtotal - totalDiscount
            });
        }

        private Guid GetCurrentUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
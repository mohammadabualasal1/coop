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
            var isNewCart = cart == null;

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

            CartItem? existingItem = null;
            if (!isNewCart)
            {
                existingItem = await _dbcontext.CartItems
                    .FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.OfferId == offer.Id);
            }

            var newQuantity = (existingItem?.Quantity ?? 0) + dto.Quantity;

            if (newQuantity > availableStock)
                return BadRequest($"الكمية المتاحة من هذا العرض هي {availableStock} فقط");

            if (offer.MaximumQuantityPerCustomer != null)
            {
                var previouslyOrdered = await GetPreviouslyOrderedQuantityAsync(userId, offer.Id);
                var remaining = offer.MaximumQuantityPerCustomer.Value - previouslyOrdered;

                if (remaining <= 0)
                    return BadRequest($"لقد وصلت للحد الأقصى المسموح به من هذا العرض ({offer.MaximumQuantityPerCustomer})");

                if (newQuantity > remaining)
                    return BadRequest($"يمكنك طلب {remaining} فقط من هذا العرض، لأنك طلبت {previouslyOrdered} سابقاً");
            }

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

        [HttpPut("items/{itemId}")]
        public async Task<IActionResult> UpdateItemQuantity(Guid itemId, UpdateCartItemRequestDto dto)
        {
            var userId = GetCurrentUserId();
            var now = DateTime.UtcNow;

            if (dto.Quantity < 1)
                return BadRequest("الكمية يجب أن تكون 1 أو أكثر");

            var cart = await _dbcontext.Carts.FirstOrDefaultAsync(c => c.CustomerUserId == userId);
            if (cart == null)
                return NotFound("السلة فارغة");

            var item = await _dbcontext.CartItems
                .FirstOrDefaultAsync(ci => ci.Id == itemId && ci.CartId == cart.Id);
            if (item == null)
                return NotFound("الصنف غير موجود في السلة");

            var offer = await _dbcontext.Offers.FirstOrDefaultAsync(o => o.Id == item.OfferId);
            if (offer == null)
                return BadRequest("العرض لم يعد موجوداً");

            if (offer.Status != OfferStatus.Active || offer.StartAt > now || offer.EndAt < now)
                return BadRequest("العرض غير متاح حالياً");

            var branchOffer = await _dbcontext.BranchOffers
                .FirstOrDefaultAsync(bo => bo.OfferId == item.OfferId
                                        && bo.MerchantBranchId == cart.MerchantBranchId
                                        && bo.IsAvailable);
            if (branchOffer == null)
                return BadRequest("العرض لم يعد متاحاً في هذا الفرع");

            var availableStock = branchOffer.TotalStock - branchOffer.ReservedStock - branchOffer.SoldStock;

            if (dto.Quantity > availableStock)
                return BadRequest($"الكمية المتاحة من هذا العرض هي {availableStock} فقط");

            if (offer.MaximumQuantityPerCustomer != null)
            {
                var previouslyOrdered = await GetPreviouslyOrderedQuantityAsync(userId, offer.Id);
                var remaining = offer.MaximumQuantityPerCustomer.Value - previouslyOrdered;

                if (remaining <= 0)
                    return BadRequest($"لقد وصلت للحد الأقصى المسموح به من هذا العرض ({offer.MaximumQuantityPerCustomer})");

                if (dto.Quantity > remaining)
                    return BadRequest($"يمكنك طلب {remaining} فقط من هذا العرض، لأنك طلبت {previouslyOrdered} سابقاً");
            }

            item.Quantity = dto.Quantity;
            item.UpdatedAt = now;
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

        [HttpDelete("items/{itemId}")]
        public async Task<IActionResult> RemoveItem(Guid itemId)
        {
            var userId = GetCurrentUserId();

            var cart = await _dbcontext.Carts.FirstOrDefaultAsync(c => c.CustomerUserId == userId);
            if (cart == null)
                return NotFound("السلة فارغة");

            var item = await _dbcontext.CartItems
                .FirstOrDefaultAsync(ci => ci.Id == itemId && ci.CartId == cart.Id);
            if (item == null)
                return NotFound("الصنف غير موجود في السلة");

            _dbcontext.CartItems.Remove(item);

            var remainingCount = await _dbcontext.CartItems
                .CountAsync(ci => ci.CartId == cart.Id && ci.Id != itemId);

            if (remainingCount == 0)
                _dbcontext.Carts.Remove(cart);
            else
                cart.UpdatedAt = DateTime.UtcNow;

            await _dbcontext.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete]
        public async Task<IActionResult> ClearCart()
        {
            var userId = GetCurrentUserId();

            var cart = await _dbcontext.Carts.FirstOrDefaultAsync(c => c.CustomerUserId == userId);
            if (cart == null)
                return NoContent();

            var items = await _dbcontext.CartItems
                .Where(ci => ci.CartId == cart.Id)
                .ToListAsync();

            _dbcontext.CartItems.RemoveRange(items);
            _dbcontext.Carts.Remove(cart);

            await _dbcontext.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("validate")]
        public async Task<IActionResult> ValidateCart()
        {
            var userId = GetCurrentUserId();
            var now = DateTime.UtcNow;

            var cart = await _dbcontext.Carts.FirstOrDefaultAsync(c => c.CustomerUserId == userId);
            if (cart == null)
            {
                return Ok(new CartValidationResponse
                {
                    IsValid = false,
                    Issues = new List<string> { "السلة فارغة" },
                    Cart = new CartResponseDto
                    {
                        Id = Guid.Empty,
                        MerchantBranchId = Guid.Empty,
                        Items = new List<CartItemResponse>(),
                        Subtotal = 0,
                        TotalDiscount = 0,
                        EstimatedTotal = 0
                    }
                });
            }

            var issues = new List<string>();

            if (cart.ExpiresAt < now)
                issues.Add("انتهت صلاحية السلة، الرجاء مراجعة الأصناف");

            var branch = await _dbcontext.MerchantBranches
                .FirstOrDefaultAsync(b => b.Id == cart.MerchantBranchId);

            if (branch == null || !branch.IsActive)
                issues.Add("الفرع غير متاح حالياً");

            var cartItems = await _dbcontext.CartItems
                .Where(ci => ci.CartId == cart.Id)
                .OrderBy(ci => ci.CreatedAt)
                .Select(ci => new
                {
                    ci.Id,
                    ci.OfferId,
                    ci.Quantity,
                    ci.AddedUnitPrice,
                    OfferTitle = ci.Offer.Title,
                    ci.Offer.Status,
                    ci.Offer.StartAt,
                    ci.Offer.EndAt,
                    ci.Offer.OriginalPrice,
                    ci.Offer.DiscountedPrice,
                    ci.Offer.MaximumQuantityPerCustomer
                })
                .ToListAsync();

            if (cartItems.Count == 0)
                issues.Add("السلة فارغة");

            foreach (var item in cartItems)
            {
                if (item.Status != OfferStatus.Active || item.StartAt > now || item.EndAt < now)
                {
                    issues.Add($"العرض \"{item.OfferTitle}\" لم يعد متاحاً");
                    continue;
                }

                var branchOffer = await _dbcontext.BranchOffers
                    .FirstOrDefaultAsync(bo => bo.OfferId == item.OfferId
                                            && bo.MerchantBranchId == cart.MerchantBranchId);

                if (branchOffer == null || !branchOffer.IsAvailable)
                {
                    issues.Add($"العرض \"{item.OfferTitle}\" لم يعد متاحاً في هذا الفرع");
                    continue;
                }

                var availableStock = branchOffer.TotalStock - branchOffer.ReservedStock - branchOffer.SoldStock;

                if (availableStock < item.Quantity)
                    issues.Add($"الكمية المتاحة من \"{item.OfferTitle}\" هي {availableStock} فقط");

                if (item.MaximumQuantityPerCustomer != null)
                {
                    var previouslyOrdered = await GetPreviouslyOrderedQuantityAsync(userId, item.OfferId);
                    var remaining = item.MaximumQuantityPerCustomer.Value - previouslyOrdered;

                    if (remaining <= 0)
                        issues.Add($"لقد وصلت للحد الأقصى المسموح به من \"{item.OfferTitle}\" ({item.MaximumQuantityPerCustomer})");
                    else if (item.Quantity > remaining)
                        issues.Add($"يمكنك طلب {remaining} فقط من \"{item.OfferTitle}\"، لأنك طلبت {previouslyOrdered} سابقاً");
                }

                if (item.AddedUnitPrice != item.DiscountedPrice)
                    issues.Add($"تغيّر سعر \"{item.OfferTitle}\"، السعر الحالي {item.DiscountedPrice}");
            }

            if (branch != null && branch.IsActive)
            {
                var itemsTotal = cartItems.Sum(i => i.DiscountedPrice * i.Quantity);
                if (itemsTotal < branch.MinimumOrderAmount)
                    issues.Add($"الحد الأدنى للطلب من هذا الفرع هو {branch.MinimumOrderAmount}");
            }

            var subtotal = cartItems.Sum(i => i.OriginalPrice * i.Quantity);
            var totalDiscount = cartItems.Sum(i => (i.OriginalPrice - i.DiscountedPrice) * i.Quantity);

            return Ok(new CartValidationResponse
            {
                IsValid = issues.Count == 0,
                Issues = issues,
                Cart = new CartResponseDto
                {
                    Id = cart.Id,
                    MerchantBranchId = cart.MerchantBranchId,
                    Items = cartItems.Select(i => new CartItemResponse
                    {
                        Id = i.Id,
                        OfferId = i.OfferId,
                        Title = i.OfferTitle,
                        Quantity = i.Quantity,
                        UnitPrice = i.DiscountedPrice,
                        LineTotal = i.DiscountedPrice * i.Quantity
                    }).ToList(),
                    Subtotal = subtotal,
                    TotalDiscount = totalDiscount,
                    EstimatedTotal = subtotal - totalDiscount
                }
            });
        }

        private static readonly OrderStatus[] NonCountingOrderStatuses =
        {
            OrderStatus.Cancelled,
            OrderStatus.Rejected,
            OrderStatus.DeliveryFailed
        };

        private async Task<int> GetPreviouslyOrderedQuantityAsync(Guid customerId, Guid offerId)
        {
            return await _dbcontext.OrderItems
                .Where(oi => oi.OfferId == offerId
                          && oi.Order.CustomerUserId == customerId
                          && !NonCountingOrderStatuses.Contains(oi.Order.Status))
                .SumAsync(oi => (int?)oi.Quantity) ?? 0;
        }

        private Guid GetCurrentUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
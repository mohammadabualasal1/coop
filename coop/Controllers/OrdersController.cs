using coop.Dtos.OrdersDtos;
using coop.Enums;
using coop.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace coop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Customer")]
    public class OrdersController : ControllerBase

    {
        private CoopDbContext _dbcontext;

        public OrdersController(CoopDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder(PlaceOrderRequestDto dto)
        {
            var userId = GetCurrentUserId();
            var now = DateTime.UtcNow;

            var address = await _dbcontext.CustomerAddresses
                .FirstOrDefaultAsync(a => a.Id == dto.CustomerAddressId && a.CustomerUserId == userId);
            if (address == null)
                return NotFound("العنوان غير موجود");

            var cart = await _dbcontext.Carts.FirstOrDefaultAsync(c => c.CustomerUserId == userId);
            if (cart == null)
                return BadRequest("السلة فارغة");

            var branch = await _dbcontext.MerchantBranches
                .FirstOrDefaultAsync(b => b.Id == cart.MerchantBranchId && b.IsActive);
            if (branch == null)
                return BadRequest("الفرع غير متاح حالياً");

            var cartItems = await _dbcontext.CartItems
                .Where(ci => ci.CartId == cart.Id)
                .Include(ci => ci.Offer)
                .ToListAsync();

            if (cartItems.Count == 0)
                return BadRequest("السلة فارغة");

            using var transaction = await _dbcontext.Database.BeginTransactionAsync();

            try
            {
                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    OrderNumber = $"COOP-{now:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}",
                    CustomerUserId = userId,
                    MerchantId = branch.MerchantId,
                    MerchantBranchId = branch.Id,
                    CustomerAddressId = address.Id,
                    Status = dto.PaymentMethod == PaymentMethod.MockOnlinePayment
                        ? OrderStatus.PendingPayment
                        : OrderStatus.PendingMerchantConfirmation,
                    PaymentMethod = dto.PaymentMethod,
                    DeliveryFee = branch.BaseDeliveryFee,
                    CustomerNotes = dto.CustomerNotes,
                    PlacedAt = now,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                decimal subtotal = 0;
                decimal totalDiscount = 0;

                foreach (var cartItem in cartItems)
                {
                    var offer = cartItem.Offer;

                    if (offer.Status != OfferStatus.Active || offer.StartAt > now || offer.EndAt < now)
                        return BadRequest($"العرض \"{offer.Title}\" لم يعد متاحاً");

                    var branchOffer = await _dbcontext.BranchOffers
                        .FirstOrDefaultAsync(bo => bo.OfferId == offer.Id
                                                && bo.MerchantBranchId == branch.Id
                                                && bo.IsAvailable);
                    if (branchOffer == null)
                        return BadRequest($"العرض \"{offer.Title}\" لم يعد متاحاً في هذا الفرع");

                    var availableStock = branchOffer.TotalStock - branchOffer.ReservedStock - branchOffer.SoldStock;
                    if (availableStock < cartItem.Quantity)
                        return BadRequest($"الكمية المتاحة من \"{offer.Title}\" هي {availableStock} فقط");

                    var product = await _dbcontext.Products.FirstOrDefaultAsync(p => p.Id == offer.ProductId);
                    if (product == null)
                        return BadRequest($"المنتج المرتبط بـ \"{offer.Title}\" غير موجود");

                    var lineSubtotal = offer.OriginalPrice * cartItem.Quantity;
                    var lineTotal = offer.DiscountedPrice * cartItem.Quantity;
                    var lineDiscount = lineSubtotal - lineTotal;

                    subtotal += lineSubtotal;
                    totalDiscount += lineDiscount;

                    _dbcontext.OrderItems.Add(new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        OfferId = offer.Id,
                        ProductId = product.Id,
                        ProductNameSnapshot = product.Name,
                        OriginalUnitPrice = offer.OriginalPrice,
                        DiscountedUnitPrice = offer.DiscountedPrice,
                        Quantity = cartItem.Quantity,
                        LineSubtotal = lineSubtotal,
                        LineDiscount = lineDiscount,
                        LineTotal = lineTotal
                    });

                    branchOffer.ReservedStock += cartItem.Quantity;

                    _dbcontext.StockReservations.Add(new StockReservation
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        BranchOfferId = branchOffer.Id,
                        Quantity = cartItem.Quantity,
                        Status = StockReservationStatus.Active,
                        ExpiresAt = now.AddMinutes(30),
                        CreatedAt = now
                    });
                }

                var itemsTotal = subtotal - totalDiscount;
                if (itemsTotal < branch.MinimumOrderAmount)
                    return BadRequest($"الحد الأدنى للطلب من هذا الفرع هو {branch.MinimumOrderAmount}");

                order.Subtotal = subtotal;
                order.TotalDiscount = totalDiscount;
                order.TotalAmount = itemsTotal + order.DeliveryFee;

                _dbcontext.Orders.Add(order);

                _dbcontext.OrderStatusHistories.Add(new OrderStatusHistory
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    OldStatus = order.Status,
                    NewStatus = order.Status,
                    ChangedByUserId = userId,
                    Note = "تم إنشاء الطلب",
                    CreatedAt = now
                });

                _dbcontext.CartItems.RemoveRange(cartItems);
                _dbcontext.Carts.Remove(cart);

                await _dbcontext.SaveChangesAsync();
                await transaction.CommitAsync();

                return StatusCode(201, new { order.Id, order.OrderNumber, order.Status, order.TotalAmount });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }




        [HttpGet]
        public async Task<IActionResult> GetMyOrders()
        {
            var userId = GetCurrentUserId();

            var orders = await _dbcontext.Orders
                .Where(o => o.CustomerUserId == userId)
                .OrderByDescending(o => o.PlacedAt)
                .Select(o => new
                {
                    o.Id,
                    o.OrderNumber,
                    o.Status,
                    o.PaymentMethod,
                    o.TotalAmount,
                    o.PlacedAt,
                    MerchantName = o.Merchant.Name,
                    BranchName = o.MerchantBranch.Name
                })
                .ToListAsync();

            return Ok(orders);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(Guid id)
        {
            var userId = GetCurrentUserId();

            var order = await _dbcontext.Orders
                .FirstOrDefaultAsync(o => o.Id == id && o.CustomerUserId == userId);

            if (order == null)
                return NotFound("الطلب غير موجود");

            var items = await _dbcontext.OrderItems
                .Where(oi => oi.OrderId == id)
                .Select(oi => new OrderItemResponseDto
                {
                    Id = oi.Id,
                    ProductNameSnapshot = oi.ProductNameSnapshot,
                    OriginalUnitPrice = oi.OriginalUnitPrice,
                    DiscountedUnitPrice = oi.DiscountedUnitPrice,
                    Quantity = oi.Quantity,
                    LineTotal = oi.LineTotal
                })
                .ToListAsync();

            return Ok(new OrderDetailResponseDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                Status = order.Status,
                PaymentMethod = order.PaymentMethod,
                Subtotal = order.Subtotal,
                TotalDiscount = order.TotalDiscount,
                DeliveryFee = order.DeliveryFee,
                TotalAmount = order.TotalAmount,
                CustomerNotes = order.CustomerNotes,
                PlacedAt = order.PlacedAt,
                AcceptedAt = order.AcceptedAt,
                ReadyAt = order.ReadyAt,
                DeliveredAt = order.DeliveredAt,
                CompletedAt = order.CompletedAt,
                Items = items
            });
        }
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelOrder(Guid id, CancelOrderRequestDto dto)
        {
            var userId = GetCurrentUserId();
            var now = DateTime.UtcNow;

            var order = await _dbcontext.Orders
                .FirstOrDefaultAsync(o => o.Id == id && o.CustomerUserId == userId);

            if (order == null)
                return NotFound("الطلب غير موجود");

            var cancellableStatuses = new[]
            {
        OrderStatus.PendingPayment,
        OrderStatus.PendingMerchantConfirmation,
        OrderStatus.Accepted
    };

            if (!cancellableStatuses.Contains(order.Status))
                return BadRequest("لا يمكن إلغاء الطلب في هذه المرحلة");

            using var transaction = await _dbcontext.Database.BeginTransactionAsync();

            try
            {
                var reservations = await _dbcontext.StockReservations
                    .Where(sr => sr.OrderId == order.Id && sr.Status == StockReservationStatus.Active)
                    .Include(sr => sr.BranchOffer)
                    .ToListAsync();

                foreach (var reservation in reservations)
                {
                    reservation.BranchOffer.ReservedStock -= reservation.Quantity;
                    reservation.Status = StockReservationStatus.Released;
                    reservation.ReleasedAt = now;
                }

                var oldStatus = order.Status;
                order.Status = OrderStatus.Cancelled;
                order.CancellationReason = dto.Reason;
                order.UpdatedAt = now;

                _dbcontext.OrderStatusHistories.Add(new OrderStatusHistory
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    OldStatus = oldStatus,
                    NewStatus = OrderStatus.Cancelled,
                    ChangedByUserId = userId,
                    Note = dto.Reason ?? "ألغى الزبون الطلب",
                    CreatedAt = now
                });

                await _dbcontext.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { order.Id, order.OrderNumber, order.Status });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        [HttpGet("{id}/tracking")]
        public async Task<IActionResult> GetOrderTracking(Guid id)
        {
            var userId = GetCurrentUserId();

            var order = await _dbcontext.Orders
                .FirstOrDefaultAsync(o => o.Id == id && o.CustomerUserId == userId);

            if (order == null)
                return NotFound("الطلب غير موجود");

            var history = await _dbcontext.OrderStatusHistories
                .Where(h => h.OrderId == id)
                .OrderBy(h => h.CreatedAt)
                .Select(h => new OrderStatusHistoryResponseDto
                {
                    OldStatus = h.OldStatus,
                    NewStatus = h.NewStatus,
                    Note = h.Note,
                    CreatedAt = h.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                order.Id,
                order.OrderNumber,
                order.Status,
                order.PlacedAt,
                order.AcceptedAt,
                order.ReadyAt,
                order.DeliveredAt,
                order.CompletedAt,
                History = history
            });
        }
        private Guid GetCurrentUserId() =>
         Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}

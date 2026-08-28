using coop.Dtos.MerchantOrdersController;
using coop.Dtos.MerchantOrdersDtos;
using coop.Enums;
using coop.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace coop.Controllers
{
    [Route("api/merchant-orders")]
    [ApiController]
    [Authorize(Roles = "Merchant")]
    public class MerchantOrdersController : ControllerBase
    {
        private readonly CoopDbContext _dbcontext;

        public MerchantOrdersController(CoopDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyOrders([FromQuery] OrderStatus? status)
        {
            var userId = GetCurrentUserId();

            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
            if (merchant == null)
                return NotFound("لا يوجد بروفايل تاجر مرتبط بحسابك");

            var query = _dbcontext.Orders
                .Where(o => o.MerchantId == merchant.Id)
                .Where(o => o.Status != OrderStatus.PendingPayment);

            if (status != null)
                query = query.Where(o => o.Status == status);

            var orders = await query
                .OrderByDescending(o => o.PlacedAt)
                .Select(o => new MerchantOrderResponse
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    CustomerName = o.CustomerUser.FullName,
                    Status = o.Status,
                    TotalAmount = o.TotalAmount,
                    PlacedAt = o.PlacedAt
                })
                .ToListAsync();

            return Ok(orders);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(Guid id)
        {
            var userId = GetCurrentUserId();

            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
            if (merchant == null)
                return NotFound("لا يوجد بروفايل تاجر مرتبط بحسابك");

            var order = await _dbcontext.Orders
                .Where(o => o.Id == id && o.MerchantId == merchant.Id)
                .Select(o => new MerchantOrderDetailResponseDto
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    CustomerName = o.CustomerUser.FullName,
                    CustomerPhone = o.CustomerUser.PhoneNumber,
                    Status = o.Status,
                    PaymentMethod = o.PaymentMethod,
                    Subtotal = o.Subtotal,
                    TotalDiscount = o.TotalDiscount,
                    DeliveryFee = o.DeliveryFee,
                    TotalAmount = o.TotalAmount,
                    CustomerNotes = o.CustomerNotes,
                    PlacedAt = o.PlacedAt,
                    AcceptedAt = o.AcceptedAt,
                    ReadyAt = o.ReadyAt,
                    Items = _dbcontext.OrderItems
                        .Where(oi => oi.OrderId == o.Id)
                        .Select(oi => new MerchantOrderItemResponseDto
                        {
                            Id = oi.Id,
                            ProductName = oi.ProductNameSnapshot,
                            Quantity = oi.Quantity,
                            DiscountedUnitPrice = oi.DiscountedUnitPrice,
                            LineTotal = oi.LineTotal
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (order == null)
                return NotFound("الطلب غير موجود");

            return Ok(order);
        }

        [HttpPost("{id}/accept")]
        public async Task<IActionResult> AcceptOrder(Guid id)
        {
            var userId = GetCurrentUserId();
            var now = DateTime.UtcNow;

            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
            if (merchant == null)
                return NotFound("لا يوجد بروفايل تاجر مرتبط بحسابك");

            var order = await _dbcontext.Orders
                .FirstOrDefaultAsync(o => o.Id == id && o.MerchantId == merchant.Id);

            if (order == null)
                return NotFound("الطلب غير موجود");

            if (order.Status != OrderStatus.PendingMerchantConfirmation)
                return BadRequest("لا يمكن قبول هذا الطلب في حالته الحالية");

            var oldStatus = order.Status;

            order.Status = OrderStatus.Preparing;
            order.AcceptedAt = now;
            order.UpdatedAt = now;

            _dbcontext.OrderStatusHistories.Add(new OrderStatusHistory
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                OldStatus = oldStatus,
                NewStatus = OrderStatus.Preparing,
                ChangedByUserId = userId,
                CreatedAt = now
            });

            var existingTask = await _dbcontext.DeliveryTasks
                .AnyAsync(t => t.OrderId == order.Id);

            if (!existingTask)
            {
                _dbcontext.DeliveryTasks.Add(new DeliveryTask
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    DriverProfileId = null,
                    PickupBranchId = order.MerchantBranchId,
                    CustomerAddressId = order.CustomerAddressId,
                    Status = DeliveryStatus.SearchingDriver,
                    DeliveryFee = order.DeliveryFee,
                    DriverEarning = order.DeliveryFee,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            await _dbcontext.SaveChangesAsync();

            return Ok(new MerchantOrderResponse
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                CustomerName = await _dbcontext.Users
                    .Where(u => u.Id == order.CustomerUserId)
                    .Select(u => u.FullName)
                    .FirstAsync(),
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                PlacedAt = order.PlacedAt
            });
        }

        [HttpPost("{id}/reject")]
        public async Task<IActionResult> RejectOrder(Guid id, RejectOrderRequestDto dto)
        {
            var userId = GetCurrentUserId();
            var now = DateTime.UtcNow;

            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
            if (merchant == null)
                return NotFound("لا يوجد بروفايل تاجر مرتبط بحسابك");

            var order = await _dbcontext.Orders
                .FirstOrDefaultAsync(o => o.Id == id && o.MerchantId == merchant.Id);

            if (order == null)
                return NotFound("الطلب غير موجود");

            if (order.Status != OrderStatus.PendingMerchantConfirmation)
                return BadRequest("لا يمكن رفض هذا الطلب في حالته الحالية");

            var reservations = await _dbcontext.StockReservations
                .Where(r => r.OrderId == order.Id && r.Status == StockReservationStatus.Active)
                .ToListAsync();

            foreach (var reservation in reservations)
            {
                var branchOffer = await _dbcontext.BranchOffers
                    .FirstOrDefaultAsync(bo => bo.Id == reservation.BranchOfferId);

                if (branchOffer != null)
                    branchOffer.ReservedStock -= reservation.Quantity;

                reservation.Status = StockReservationStatus.Released;
                reservation.ReleasedAt = now;
            }

            var payment = await _dbcontext.Payments
                .FirstOrDefaultAsync(p => p.OrderId == order.Id);

            if (payment != null && payment.Status == PaymentStatus.Paid)
            {
                payment.Status = PaymentStatus.Refunded;
                payment.RefundedAt = now;
                payment.FailureReason = "تم رفض الطلب من قبل التاجر";
                payment.UpdatedAt = now;
            }

            var oldStatus = order.Status;

            order.Status = OrderStatus.Rejected;
            order.MerchantRejectionReason = dto.Reason;
            order.UpdatedAt = now;

            _dbcontext.OrderStatusHistories.Add(new OrderStatusHistory
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                OldStatus = oldStatus,
                NewStatus = OrderStatus.Rejected,
                ChangedByUserId = userId,
                Note = dto.Reason,
                CreatedAt = now
            });

            await _dbcontext.SaveChangesAsync();

            return Ok(new MerchantOrderResponse
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                CustomerName = await _dbcontext.Users
                    .Where(u => u.Id == order.CustomerUserId)
                    .Select(u => u.FullName)
                    .FirstAsync(),
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                PlacedAt = order.PlacedAt
            });
        }
        [HttpPost("{id}/ready")]
        public async Task<IActionResult> MarkReadyForPickup(Guid id)
        {
            var userId = GetCurrentUserId();
            var now = DateTime.UtcNow;

            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
            if (merchant == null)
                return NotFound("لا يوجد بروفايل تاجر مرتبط بحسابك");

            var order = await _dbcontext.Orders
                .FirstOrDefaultAsync(o => o.Id == id && o.MerchantId == merchant.Id);

            if (order == null)
                return NotFound("الطلب غير موجود");

            if (order.Status != OrderStatus.Preparing)
                return BadRequest("يجب أن يكون الطلب قيد التجهيز أولاً");

            var oldStatus = order.Status;

            order.Status = OrderStatus.ReadyForPickup;
            order.ReadyAt = now;
            order.UpdatedAt = now;

            _dbcontext.OrderStatusHistories.Add(new OrderStatusHistory
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                OldStatus = oldStatus,
                NewStatus = OrderStatus.ReadyForPickup,
                ChangedByUserId = userId,
                CreatedAt = now
            });

            // إنشاء مهمة التوصيل ليلتقطها DriverMatchingService
            var taskExists = await _dbcontext.DeliveryTasks.AnyAsync(t => t.OrderId == order.Id);
            if (!taskExists)
            {
                _dbcontext.DeliveryTasks.Add(new DeliveryTask
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    DriverProfileId = null,
                    PickupBranchId = order.MerchantBranchId,
                    CustomerAddressId = order.CustomerAddressId,
                    Status = DeliveryStatus.SearchingDriver,
                    DeliveryFee = order.DeliveryFee,
                    DriverEarning = order.DeliveryFee,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            await _dbcontext.SaveChangesAsync();

            return Ok(new MerchantOrderResponse
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                CustomerName = await _dbcontext.Users
                    .Where(u => u.Id == order.CustomerUserId)
                    .Select(u => u.FullName)
                    .FirstAsync(),
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                PlacedAt = order.PlacedAt
            });
        }
        [HttpPost("{id}/pickup-code")]
        public async Task<IActionResult> GetPickupCode(Guid id)
        {
            var userId = GetCurrentUserId();
            var now = DateTime.UtcNow;

            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
            if (merchant == null)
                return NotFound("لا يوجد بروفايل تاجر مرتبط بحسابك");

            var order = await _dbcontext.Orders
                .FirstOrDefaultAsync(o => o.Id == id && o.MerchantId == merchant.Id);

            if (order == null)
                return NotFound("الطلب غير موجود");

            if (order.Status != OrderStatus.ReadyForPickup && order.Status != OrderStatus.DriverAssigned)
                return BadRequest("يجب أن يكون الطلب جاهزاً للاستلام أولاً");

            var task = await _dbcontext.DeliveryTasks
                .FirstOrDefaultAsync(t => t.OrderId == order.Id);

            if (task == null)
                return BadRequest("لا توجد مهمة توصيل لهذا الطلب");

            var oldTokens = await _dbcontext.ConfirmationTokens
                .Where(t => t.DeliveryTaskId == task.Id
                         && t.Type == ConfirmationTokenType.MerchantPickup
                         && t.UsedAt == null
                         && !t.IsRevoked)
                .ToListAsync();

            foreach (var oldToken in oldTokens)
                oldToken.IsRevoked = true;

            var code = RandomNumberGenerator.GetInt32(0, 1000000).ToString("D6");
            var expiresAt = now.AddMinutes(30);

            _dbcontext.ConfirmationTokens.Add(new ConfirmationToken
            {
                Id = Guid.NewGuid(),
                DeliveryTaskId = task.Id,
                Type = ConfirmationTokenType.MerchantPickup,
                TokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(code))),
                ExpiresAt = expiresAt,
                IsRevoked = false,
                CreatedAt = now
            });

            await _dbcontext.SaveChangesAsync();

            return Ok(new PickupCodeResponse
            {
                Code = code,
                ExpiresAt = expiresAt
            });
        }
        private Guid GetCurrentUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
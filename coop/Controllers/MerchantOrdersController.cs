using coop.Dtos.MerchantOrdersController;
using coop.Dtos.MerchantOrdersDtos;
using coop.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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


        private Guid GetCurrentUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
using coop.Dtos.PaymentsController;
using coop.Enums;
using coop.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace coop.Controllers
{
    [Route("api/payments")]
    [ApiController]
    [Authorize(Roles = "Customer")]
    public class PaymentsController : ControllerBase
    {
        private readonly CoopDbContext _dbcontext;

        public PaymentsController(CoopDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }

        [HttpPost("mock-charge")]
        public async Task<IActionResult> MockCharge(MockPaymentRequestDto dto)
        {
            var userId = GetCurrentUserId();
            var now = DateTime.UtcNow;

            var order = await _dbcontext.Orders
                .FirstOrDefaultAsync(o => o.Id == dto.OrderId && o.CustomerUserId == userId);

            if (order == null)
                return NotFound("الطلب غير موجود");

            if (order.PaymentMethod != PaymentMethod.MockOnlinePayment)
                return BadRequest("هذا الطلب ليس بالدفع الإلكتروني");

            if (order.Status != OrderStatus.PendingPayment)
                return BadRequest("لا يمكن الدفع لهذا الطلب في حالته الحالية");

            var payment = await _dbcontext.Payments
                .FirstOrDefaultAsync(p => p.OrderId == order.Id);

            if (payment == null)
            {
                payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    Method = PaymentMethod.MockOnlinePayment,
                    Status = PaymentStatus.Pending,
                    Amount = order.TotalAmount,
                    MockProvider = "MockGateway",
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _dbcontext.Payments.Add(payment);
            }

            if (payment.Status == PaymentStatus.Paid)
                return BadRequest("تم دفع هذا الطلب مسبقاً");

            var isSuccess = dto.SimulatedResult != null
                         && dto.SimulatedResult.Trim().ToLower() == "success";

            if (isSuccess)
            {
                payment.Status = PaymentStatus.Paid;
                payment.TransactionReference = $"MOCK-{Guid.NewGuid().ToString("N")[..12].ToUpper()}";
                payment.PaidAt = now;
                payment.FailureReason = null;
                payment.UpdatedAt = now;

                var oldStatus = order.Status;
                order.Status = OrderStatus.PendingMerchantConfirmation;
                order.UpdatedAt = now;

                _dbcontext.OrderStatusHistories.Add(new OrderStatusHistory
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    OldStatus = oldStatus,
                    NewStatus = OrderStatus.PendingMerchantConfirmation,
                    ChangedByUserId = userId,
                    Note = "تم الدفع بنجاح",
                    CreatedAt = now
                });
            }
            else
            {
                payment.Status = PaymentStatus.Failed;
                payment.FailureReason = "فشلت عملية الدفع الوهمية";
                payment.UpdatedAt = now;
            }

            await _dbcontext.SaveChangesAsync();

            return Ok(new PaymentResponse
            {
                Id = payment.Id,
                OrderId = payment.OrderId,
                Method = payment.Method,
                Status = payment.Status,
                Amount = payment.Amount,
                TransactionReference = payment.TransactionReference,
                PaidAt = payment.PaidAt
            });
        }

        private Guid GetCurrentUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
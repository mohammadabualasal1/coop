using coop.Dtos.DeliveryTasksController;
using coop.Dtos.DriverTaskOffersDtos;
using coop.Dtos.DriverTaskOffersDtos;
using coop.Enums;
using coop.Hubs;
using coop.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace coop.Controllers
{
    [Route("api/delivery-tasks")]
    [ApiController]
    [Authorize(Roles = "Driver")]
    public class DeliveryTasksController : ControllerBase
    {
        private readonly CoopDbContext _dbcontext;
        private readonly IHubContext<TrackingHub> _hubContext;

        public DeliveryTasksController(CoopDbContext dbcontext, IHubContext<TrackingHub> hubContext)
        {
            _dbcontext = dbcontext;
            _hubContext = hubContext;
        }

        [HttpGet("offers")]
        public async Task<IActionResult> GetAvailableOffers()
        {
            var userId = GetCurrentUserId();
            var now = DateTime.UtcNow;

            var driverProfile = await _dbcontext.DriverProfiles
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (driverProfile == null)
                return NotFound("لا يوجد بروفايل سائق مرتبط بحسابك");

            if (driverProfile.VerificationStatus != VerificationStatus.Approved)
                return BadRequest("يجب توثيق حسابك قبل استقبال المهام");

            var offers = await _dbcontext.DriverTaskOffers
                .Where(o => o.DriverProfileId == driverProfile.Id)
                .Where(o => o.Status == DriverTaskOfferStatus.Pending)
                .Where(o => o.ExpiresAt > now)
                .OrderBy(o => o.ExpiresAt)
                .Select(o => new DriverTaskOfferResponseDto
                {
                    Id = o.Id,
                    DeliveryTaskId = o.DeliveryTaskId,
                    MerchantBranchName = o.DeliveryTask.PickupBranch.Name,
                    CustomerCity = o.DeliveryTask.CustomerAddress.City,
                    DeliveryFee = o.DeliveryTask.DeliveryFee,
                    ExpiresAt = o.ExpiresAt
                })
                .ToListAsync();

            return Ok(offers);
        }

        [HttpPost("offers/{id}/accept")]
        public async Task<IActionResult> AcceptOffer(Guid id)
        {
            var userId = GetCurrentUserId();
            var now = DateTime.UtcNow;

            var driverProfile = await _dbcontext.DriverProfiles
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (driverProfile == null)
                return NotFound("لا يوجد بروفايل سائق مرتبط بحسابك");

            if (driverProfile.VerificationStatus != VerificationStatus.Approved)
                return BadRequest("يجب توثيق حسابك قبل قبول المهام");

            var offer = await _dbcontext.DriverTaskOffers
                .FirstOrDefaultAsync(o => o.Id == id && o.DriverProfileId == driverProfile.Id);

            if (offer == null)
                return NotFound("العرض غير موجود");

            if (offer.Status != DriverTaskOfferStatus.Pending)
                return BadRequest("تم الرد على هذا العرض مسبقاً");

            if (offer.ExpiresAt < now)
            {
                offer.Status = DriverTaskOfferStatus.Expired;
                await _dbcontext.SaveChangesAsync();
                return BadRequest("انتهت مهلة هذا العرض");
            }

            var task = await _dbcontext.DeliveryTasks
                .FirstOrDefaultAsync(t => t.Id == offer.DeliveryTaskId);

            if (task == null)
                return NotFound("مهمة التوصيل غير موجودة");

            if (task.DriverProfileId != null)
                return BadRequest("تم إسناد هذه المهمة لسائق آخر");

            var hasActiveTask = await _dbcontext.DeliveryTasks
                .AnyAsync(t => t.DriverProfileId == driverProfile.Id
                            && t.Status != DeliveryStatus.Delivered
                            && t.Status != DeliveryStatus.Failed
                            && t.Status != DeliveryStatus.Cancelled);

            if (hasActiveTask)
                return BadRequest("لديك مهمة نشطة بالفعل، أنهها أولاً");

            offer.Status = DriverTaskOfferStatus.Accepted;
            offer.RespondedAt = now;

            task.DriverProfileId = driverProfile.Id;
            task.Status = DeliveryStatus.Assigned;
            task.AssignedAt = now;
            task.UpdatedAt = now;

            var otherOffers = await _dbcontext.DriverTaskOffers
                .Where(o => o.DeliveryTaskId == task.Id
                         && o.Id != offer.Id
                         && o.Status == DriverTaskOfferStatus.Pending)
                .ToListAsync();

            foreach (var other in otherOffers)
            {
                other.Status = DriverTaskOfferStatus.Expired;
                other.RespondedAt = now;
            }

            var order = await _dbcontext.Orders.FirstOrDefaultAsync(o => o.Id == task.OrderId);

            if (order != null)
            {
                var oldStatus = order.Status;
                order.Status = OrderStatus.DriverAssigned;
                order.UpdatedAt = now;

                _dbcontext.OrderStatusHistories.Add(new OrderStatusHistory
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    OldStatus = oldStatus,
                    NewStatus = OrderStatus.DriverAssigned,
                    ChangedByUserId = userId,
                    CreatedAt = now
                });
            }

            await _dbcontext.SaveChangesAsync();

            var driverUser = await _dbcontext.Users.FirstAsync(u => u.Id == userId);

            await _hubContext.Clients
                .Group(TrackingHub.OrderGroup(task.OrderId))
                .SendAsync("delivery.driver.assigned", new
                {
                    OrderId = task.OrderId,
                    DeliveryTaskId = task.Id,
                    DriverName = driverUser.FullName,
                    DriverPhone = driverUser.PhoneNumber,
                    driverProfile.VehicleType,
                    driverProfile.VehiclePlateNumber,
                    AssignedAt = now
                });

            if (order != null)
            {
                await _hubContext.Clients
                    .Group(TrackingHub.OrderGroup(task.OrderId))
                    .SendAsync("order.status.changed", new
                    {
                        OrderId = order.Id,
                        order.OrderNumber,
                        NewStatus = order.Status,
                        ChangedAt = now
                    });
            }

            return Ok(new DeliveryTaskResponseDto
            {
                Id = task.Id,
                OrderId = task.OrderId,
                Status = task.Status,
                PickupBranchId = task.PickupBranchId,
                CustomerAddressId = task.CustomerAddressId,
                DeliveryFee = task.DeliveryFee,
                DriverEarning = task.DriverEarning
            });
        }

        [HttpPost("offers/{id}/decline")]
        public async Task<IActionResult> DeclineOffer(Guid id, DeclineOfferRequestDto dto)
        {
            var userId = GetCurrentUserId();
            var now = DateTime.UtcNow;

            var driverProfile = await _dbcontext.DriverProfiles
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (driverProfile == null)
                return NotFound("لا يوجد بروفايل سائق مرتبط بحسابك");

            var offer = await _dbcontext.DriverTaskOffers
                .FirstOrDefaultAsync(o => o.Id == id && o.DriverProfileId == driverProfile.Id);

            if (offer == null)
                return NotFound("العرض غير موجود");

            if (offer.Status != DriverTaskOfferStatus.Pending)
                return BadRequest("تم الرد على هذا العرض مسبقاً");

            offer.Status = DriverTaskOfferStatus.Rejected;
            offer.RespondedAt = now;
            offer.RejectionReason = dto.Reason;

            await _dbcontext.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyTasks()
        {
            var userId = GetCurrentUserId();

            var driverProfile = await _dbcontext.DriverProfiles
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (driverProfile == null)
                return NotFound("لا يوجد بروفايل سائق مرتبط بحسابك");

            var tasks = await _dbcontext.DeliveryTasks
                .Where(t => t.DriverProfileId == driverProfile.Id)
                .Where(t => t.Status != DeliveryStatus.Delivered
                         && t.Status != DeliveryStatus.Failed
                         && t.Status != DeliveryStatus.Cancelled)
                .OrderByDescending(t => t.AssignedAt)
                .Select(t => new DeliveryTaskResponseDto
                {
                    Id = t.Id,
                    OrderId = t.OrderId,
                    Status = t.Status,
                    PickupBranchId = t.PickupBranchId,
                    CustomerAddressId = t.CustomerAddressId,
                    DeliveryFee = t.DeliveryFee,
                    DriverEarning = t.DriverEarning
                })
                .ToListAsync();

            return Ok(tasks);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTaskById(Guid id)
        {
            var userId = GetCurrentUserId();

            var driverProfile = await _dbcontext.DriverProfiles
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (driverProfile == null)
                return NotFound("لا يوجد بروفايل سائق مرتبط بحسابك");

            var task = await _dbcontext.DeliveryTasks
                .Where(t => t.Id == id && t.DriverProfileId == driverProfile.Id)
                .Select(t => new DeliveryTaskDetailResponseDto
                {
                    Id = t.Id,
                    OrderId = t.OrderId,
                    OrderNumber = t.Order.OrderNumber,
                    Status = t.Status,
                    DeliveryFee = t.DeliveryFee,
                    DriverEarning = t.DriverEarning,
                    BranchName = t.PickupBranch.Name,
                    BranchAddress = t.PickupBranch.Address,
                    BranchPhone = t.PickupBranch.PhoneNumber,
                    BranchLatitude = t.PickupBranch.Latitude,
                    BranchLongitude = t.PickupBranch.Longitude,
                    CustomerName = t.CustomerAddress.ContactName,
                    CustomerPhone = t.CustomerAddress.ContactPhone,
                    CustomerCity = t.CustomerAddress.City,
                    CustomerArea = t.CustomerAddress.Area,
                    CustomerStreet = t.CustomerAddress.Street,
                    CustomerBuilding = t.CustomerAddress.Building,
                    CustomerFloor = t.CustomerAddress.Floor,
                    AdditionalDirections = t.CustomerAddress.AdditionalDirections,
                    CustomerLatitude = t.CustomerAddress.Latitude,
                    CustomerLongitude = t.CustomerAddress.Longitude,
                    PaymentMethod = t.Order.PaymentMethod,
                    AmountToCollect = t.Order.PaymentMethod == PaymentMethod.CashOnDelivery
                        ? t.Order.TotalAmount
                        : 0,
                    AssignedAt = t.AssignedAt,
                    ArrivedAtMerchantAt = t.ArrivedAtMerchantAt,
                    PickedUpAt = t.PickedUpAt,
                    ArrivedAtCustomerAt = t.ArrivedAtCustomerAt
                })
                .FirstOrDefaultAsync();

            if (task == null)
                return NotFound("المهمة غير موجودة");

            return Ok(task);
        }

        [HttpPost("{id}/arrived-at-merchant")]
        public async Task<IActionResult> ArrivedAtMerchant(Guid id)
        {
            var userId = GetCurrentUserId();
            var now = DateTime.UtcNow;

            var driverProfile = await _dbcontext.DriverProfiles
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (driverProfile == null)
                return NotFound("لا يوجد بروفايل سائق مرتبط بحسابك");

            var task = await _dbcontext.DeliveryTasks
                .FirstOrDefaultAsync(t => t.Id == id && t.DriverProfileId == driverProfile.Id);

            if (task == null)
                return NotFound("المهمة غير موجودة");

            if (task.Status != DeliveryStatus.Assigned && task.Status != DeliveryStatus.GoingToMerchant)
                return BadRequest("لا يمكن تنفيذ هذا الإجراء في الحالة الحالية");

            task.Status = DeliveryStatus.ArrivedAtMerchant;
            task.ArrivedAtMerchantAt = now;
            task.UpdatedAt = now;

            await _dbcontext.SaveChangesAsync();

            await _hubContext.Clients
                .Group(TrackingHub.OrderGroup(task.OrderId))
                .SendAsync("delivery.status.changed", new
                {
                    OrderId = task.OrderId,
                    DeliveryTaskId = task.Id,
                    NewStatus = task.Status,
                    ChangedAt = now
                });

            return Ok(new DeliveryTaskResponseDto
            {
                Id = task.Id,
                OrderId = task.OrderId,
                Status = task.Status,
                PickupBranchId = task.PickupBranchId,
                CustomerAddressId = task.CustomerAddressId,
                DeliveryFee = task.DeliveryFee,
                DriverEarning = task.DriverEarning
            });
        }

        [HttpPost("{id}/confirm-pickup")]
        public async Task<IActionResult> ConfirmPickup(Guid id, ConfirmPickupRequestDto dto)
        {
            var userId = GetCurrentUserId();
            var now = DateTime.UtcNow;

            var driverProfile = await _dbcontext.DriverProfiles
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (driverProfile == null)
                return NotFound("لا يوجد بروفايل سائق مرتبط بحسابك");

            var task = await _dbcontext.DeliveryTasks
                .FirstOrDefaultAsync(t => t.Id == id && t.DriverProfileId == driverProfile.Id);

            if (task == null)
                return NotFound("المهمة غير موجودة");

            if (task.Status != DeliveryStatus.ArrivedAtMerchant)
                return BadRequest("يجب تسجيل الوصول للفرع أولاً");

            var order = await _dbcontext.Orders.FirstOrDefaultAsync(o => o.Id == task.OrderId);

            if (order != null && order.Status != OrderStatus.ReadyForPickup && order.Status != OrderStatus.DriverAssigned)
                return BadRequest("الطلب لم يجهّز بعد");

            var codeHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(dto.Code)));

            var token = await _dbcontext.ConfirmationTokens
                .FirstOrDefaultAsync(t => t.DeliveryTaskId == task.Id
                                       && t.Type == ConfirmationTokenType.MerchantPickup
                                       && t.TokenHash == codeHash
                                       && t.UsedAt == null
                                       && !t.IsRevoked);

            if (token == null)
                return BadRequest("كود الاستلام غير صحيح");

            if (token.ExpiresAt < now)
                return BadRequest("انتهت صلاحية كود الاستلام، اطلب كوداً جديداً من التاجر");

            token.UsedAt = now;
            token.UsedByUserId = userId;

            task.Status = DeliveryStatus.PickedUp;
            task.PickedUpAt = now;
            task.UpdatedAt = now;

            if (order != null)
            {
                var oldStatus = order.Status;
                order.Status = OrderStatus.OutForDelivery;
                order.UpdatedAt = now;

                _dbcontext.OrderStatusHistories.Add(new OrderStatusHistory
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    OldStatus = oldStatus,
                    NewStatus = OrderStatus.OutForDelivery,
                    ChangedByUserId = userId,
                    CreatedAt = now
                });
            }

            await _dbcontext.SaveChangesAsync();

            await _hubContext.Clients
                .Group(TrackingHub.OrderGroup(task.OrderId))
                .SendAsync("delivery.status.changed", new
                {
                    OrderId = task.OrderId,
                    DeliveryTaskId = task.Id,
                    NewStatus = task.Status,
                    ChangedAt = now
                });

            if (order != null)
            {
                await _hubContext.Clients
                    .Group(TrackingHub.OrderGroup(task.OrderId))
                    .SendAsync("order.status.changed", new
                    {
                        OrderId = order.Id,
                        order.OrderNumber,
                        NewStatus = order.Status,
                        ChangedAt = now
                    });
            }

            return Ok(new DeliveryTaskResponseDto
            {
                Id = task.Id,
                OrderId = task.OrderId,
                Status = task.Status,
                PickupBranchId = task.PickupBranchId,
                CustomerAddressId = task.CustomerAddressId,
                DeliveryFee = task.DeliveryFee,
                DriverEarning = task.DriverEarning
            });
        }

        [HttpPost("{id}/arrived-at-customer")]
        public async Task<IActionResult> ArrivedAtCustomer(Guid id)
        {
            var userId = GetCurrentUserId();
            var now = DateTime.UtcNow;

            var driverProfile = await _dbcontext.DriverProfiles
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (driverProfile == null)
                return NotFound("لا يوجد بروفايل سائق مرتبط بحسابك");

            var task = await _dbcontext.DeliveryTasks
                .FirstOrDefaultAsync(t => t.Id == id && t.DriverProfileId == driverProfile.Id);

            if (task == null)
                return NotFound("المهمة غير موجودة");

            if (task.Status != DeliveryStatus.PickedUp && task.Status != DeliveryStatus.GoingToCustomer)
                return BadRequest("يجب تأكيد استلام الطلب من الفرع أولاً");

            task.Status = DeliveryStatus.ArrivedAtCustomer;
            task.ArrivedAtCustomerAt = now;
            task.UpdatedAt = now;

            await _dbcontext.SaveChangesAsync();

            await _hubContext.Clients
                .Group(TrackingHub.OrderGroup(task.OrderId))
                .SendAsync("delivery.status.changed", new
                {
                    OrderId = task.OrderId,
                    DeliveryTaskId = task.Id,
                    NewStatus = task.Status,
                    ChangedAt = now
                });

            return Ok(new DeliveryTaskResponseDto
            {
                Id = task.Id,
                OrderId = task.OrderId,
                Status = task.Status,
                PickupBranchId = task.PickupBranchId,
                CustomerAddressId = task.CustomerAddressId,
                DeliveryFee = task.DeliveryFee,
                DriverEarning = task.DriverEarning
            });
        }

        [HttpPost("{id}/complete")]
        public async Task<IActionResult> CompleteDelivery(Guid id, ConfirmDeliveryRequestDto dto)
        {
            var userId = GetCurrentUserId();
            var now = DateTime.UtcNow;

            var driverProfile = await _dbcontext.DriverProfiles
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (driverProfile == null)
                return NotFound("لا يوجد بروفايل سائق مرتبط بحسابك");

            var task = await _dbcontext.DeliveryTasks
                .FirstOrDefaultAsync(t => t.Id == id && t.DriverProfileId == driverProfile.Id);

            if (task == null)
                return NotFound("المهمة غير موجودة");

            if (task.Status != DeliveryStatus.ArrivedAtCustomer)
                return BadRequest("يجب تسجيل الوصول للزبون أولاً");

            var codeHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(dto.Code)));

            var token = await _dbcontext.ConfirmationTokens
                .FirstOrDefaultAsync(t => t.DeliveryTaskId == task.Id
                                       && t.Type == ConfirmationTokenType.CustomerDelivery
                                       && t.TokenHash == codeHash
                                       && t.UsedAt == null
                                       && !t.IsRevoked);

            if (token == null)
                return BadRequest("كود التسليم غير صحيح");

            if (token.ExpiresAt < now)
                return BadRequest("انتهت صلاحية كود التسليم");

            using var transaction = await _dbcontext.Database.BeginTransactionAsync();

            try
            {
                token.UsedAt = now;
                token.UsedByUserId = userId;

                task.Status = DeliveryStatus.Delivered;
                task.DeliveredAt = now;
                task.UpdatedAt = now;

                var order = await _dbcontext.Orders.FirstOrDefaultAsync(o => o.Id == task.OrderId);

                if (order != null)
                {
                    var oldStatus = order.Status;
                    order.Status = OrderStatus.Delivered;
                    order.DeliveredAt = now;
                    order.UpdatedAt = now;

                    _dbcontext.OrderStatusHistories.Add(new OrderStatusHistory
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        OldStatus = oldStatus,
                        NewStatus = OrderStatus.Delivered,
                        ChangedByUserId = userId,
                        CreatedAt = now
                    });

                    // تحويل المخزون المحجوز إلى مباع
                    var reservations = await _dbcontext.StockReservations
                        .Where(r => r.OrderId == order.Id && r.Status == StockReservationStatus.Active)
                        .Include(r => r.BranchOffer)
                        .ToListAsync();

                    foreach (var reservation in reservations)
                    {
                        reservation.BranchOffer.ReservedStock -= reservation.Quantity;
                        reservation.BranchOffer.SoldStock += reservation.Quantity;
                        reservation.Status = StockReservationStatus.Confirmed;
                    }

                    // الدفع عند الاستلام يصبح مدفوعاً
                    var payment = await _dbcontext.Payments
                        .FirstOrDefaultAsync(p => p.OrderId == order.Id);

                    if (payment != null
                        && payment.Method == PaymentMethod.CashOnDelivery
                        && payment.Status == PaymentStatus.Pending)
                    {
                        payment.Status = PaymentStatus.Paid;
                        payment.PaidAt = now;
                        payment.UpdatedAt = now;
                    }
                }

                driverProfile.CompletedDeliveries += 1;

                await _dbcontext.SaveChangesAsync();
                await transaction.CommitAsync();

                await _hubContext.Clients
                    .Group(TrackingHub.OrderGroup(task.OrderId))
                    .SendAsync("delivery.status.changed", new
                    {
                        OrderId = task.OrderId,
                        DeliveryTaskId = task.Id,
                        NewStatus = task.Status,
                        ChangedAt = now
                    });

                if (order != null)
                {
                    await _hubContext.Clients
                        .Group(TrackingHub.OrderGroup(task.OrderId))
                        .SendAsync("order.status.changed", new
                        {
                            OrderId = order.Id,
                            order.OrderNumber,
                            NewStatus = order.Status,
                            ChangedAt = now
                        });
                }

                return Ok(new DeliveryTaskResponseDto
                {
                    Id = task.Id,
                    OrderId = task.OrderId,
                    Status = task.Status,
                    PickupBranchId = task.PickupBranchId,
                    CustomerAddressId = task.CustomerAddressId,
                    DeliveryFee = task.DeliveryFee,
                    DriverEarning = task.DriverEarning
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        [HttpPost("{id}/report-failure")]
        public async Task<IActionResult> ReportFailure(Guid id, ReportDeliveryFailureRequestDto dto)
        {
            var userId = GetCurrentUserId();
            var now = DateTime.UtcNow;

            var driverProfile = await _dbcontext.DriverProfiles
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (driverProfile == null)
                return NotFound("لا يوجد بروفايل سائق مرتبط بحسابك");

            var task = await _dbcontext.DeliveryTasks
                .FirstOrDefaultAsync(t => t.Id == id && t.DriverProfileId == driverProfile.Id);

            if (task == null)
                return NotFound("المهمة غير موجودة");

            if (task.Status == DeliveryStatus.Delivered
                || task.Status == DeliveryStatus.Failed
                || task.Status == DeliveryStatus.Cancelled)
                return BadRequest("المهمة منتهية بالفعل");

            using var transaction = await _dbcontext.Database.BeginTransactionAsync();

            try
            {
                task.Status = DeliveryStatus.Failed;
                task.FailureReason = dto.Reason;
                task.UpdatedAt = now;

                var order = await _dbcontext.Orders.FirstOrDefaultAsync(o => o.Id == task.OrderId);

                if (order != null)
                {
                    var oldStatus = order.Status;
                    order.Status = OrderStatus.DeliveryFailed;
                    order.UpdatedAt = now;

                    _dbcontext.OrderStatusHistories.Add(new OrderStatusHistory
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        OldStatus = oldStatus,
                        NewStatus = OrderStatus.DeliveryFailed,
                        ChangedByUserId = userId,
                        Note = dto.Reason,
                        CreatedAt = now
                    });

                    // تحرير المخزون المحجوز
                    var reservations = await _dbcontext.StockReservations
                        .Where(r => r.OrderId == order.Id && r.Status == StockReservationStatus.Active)
                        .Include(r => r.BranchOffer)
                        .ToListAsync();

                    foreach (var reservation in reservations)
                    {
                        reservation.BranchOffer.ReservedStock -= reservation.Quantity;
                        reservation.Status = StockReservationStatus.Released;
                        reservation.ReleasedAt = now;
                    }
                }

                await _dbcontext.SaveChangesAsync();
                await transaction.CommitAsync();

                await _hubContext.Clients
                    .Group(TrackingHub.OrderGroup(task.OrderId))
                    .SendAsync("delivery.status.changed", new
                    {
                        OrderId = task.OrderId,
                        DeliveryTaskId = task.Id,
                        NewStatus = task.Status,
                        FailureReason = dto.Reason,
                        ChangedAt = now
                    });

                if (order != null)
                {
                    await _hubContext.Clients
                        .Group(TrackingHub.OrderGroup(task.OrderId))
                        .SendAsync("order.status.changed", new
                        {
                            OrderId = order.Id,
                            order.OrderNumber,
                            NewStatus = order.Status,
                            ChangedAt = now
                        });
                }

                return Ok(new DeliveryTaskResponseDto
                {
                    Id = task.Id,
                    OrderId = task.OrderId,
                    Status = task.Status,
                    PickupBranchId = task.PickupBranchId,
                    CustomerAddressId = task.CustomerAddressId,
                    DeliveryFee = task.DeliveryFee,
                    DriverEarning = task.DriverEarning
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private Guid GetCurrentUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}

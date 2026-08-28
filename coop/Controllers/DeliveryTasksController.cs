using coop.Dtos.DeliveryTasksController;
using coop.Dtos.DriverTaskOffersController;
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
    [Route("api/delivery-tasks")]
    [ApiController]
    [Authorize(Roles = "Driver")]
    public class DeliveryTasksController : ControllerBase
    {
        private readonly CoopDbContext _dbcontext;

        public DeliveryTasksController(CoopDbContext dbcontext)
        {
            _dbcontext = dbcontext;
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

            var order = await _dbcontext.Orders.FirstOrDefaultAsync(o => o.Id == task.OrderId);

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
        private Guid GetCurrentUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
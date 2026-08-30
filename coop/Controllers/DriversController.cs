using coop.Dtos.DriversController;
using coop.Dtos.DriversDtos;
using coop.Enums;
using coop.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using coop.Hubs;
using Microsoft.AspNetCore.SignalR;
using NetTopologySuite.Geometries;
namespace coop.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = "Driver")]
    [ApiController]
    public class DriversController : ControllerBase
    {
        private readonly CoopDbContext _dbcontext;
        private readonly IHubContext<TrackingHub> _hubContext;

        public DriversController(CoopDbContext dbcontext, IHubContext<TrackingHub> hubContext)
        {
            _dbcontext = dbcontext;
            _hubContext = hubContext;
        }

        [HttpPost]
        public async Task<IActionResult> AddDriverProfile([FromBody] CreateDriverProfileRequestDto dto)
        {
            var userId = GetCurrentUserId();

            var exists = await _dbcontext.DriverProfiles.AnyAsync(d => d.UserId == userId);
            if (exists)
                return Conflict("لديك بروفايل سائق بالفعل");

            if (dto.MaximumCapacity < 1)
                return BadRequest("السعة القصوى يجب أن تكون 1 أو أكثر");

            var newProfile = new DriverProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                VehicleType = dto.VehicleType,
                VehiclePlateNumber = dto.VehiclePlateNumber,
                MaximumCapacity = dto.MaximumCapacity,
                VerificationStatus = VerificationStatus.Pending,
                IsAvailable = false,
                CompletedDeliveries = 0,
                CreatedAt = DateTime.UtcNow
            };

            _dbcontext.DriverProfiles.Add(newProfile);
            await _dbcontext.SaveChangesAsync();

            return StatusCode(201, new DriverProfileResponse
            {
                Id = newProfile.Id,
                VehicleType = newProfile.VehicleType,
                VehiclePlateNumber = newProfile.VehiclePlateNumber,
                MaximumCapacity = newProfile.MaximumCapacity,
                VerificationStatus = newProfile.VerificationStatus,
                IsAvailable = newProfile.IsAvailable,
                AverageRating = newProfile.AverageRating,
                CompletedDeliveries = newProfile.CompletedDeliveries
            });
        }



        [HttpGet("my")]
        public async Task<IActionResult> GetMyDriverProfile()
        {
            var userId = GetCurrentUserId();

            var profile = await _dbcontext.DriverProfiles.FirstOrDefaultAsync(d => d.UserId == userId);
            if (profile == null)
                return NotFound("لا يوجد بروفايل سائق مرتبط بحسابك");

            return Ok(new DriverProfileResponse
            {
                Id = profile.Id,
                VehicleType = profile.VehicleType,
                VehiclePlateNumber = profile.VehiclePlateNumber,
                MaximumCapacity = profile.MaximumCapacity,
                VerificationStatus = profile.VerificationStatus,
                IsAvailable = profile.IsAvailable,
                AverageRating = profile.AverageRating,
                CompletedDeliveries = profile.CompletedDeliveries
            });
        }
        [HttpPut("my")]
        public async Task<IActionResult> UpdateMyDriverProfile([FromBody] UpdateDriverProfileRequestDto dto)
        {
            var userId = GetCurrentUserId();

            var profile = await _dbcontext.DriverProfiles.FirstOrDefaultAsync(d => d.UserId == userId);
            if (profile == null)
                return NotFound("لا يوجد بروفايل سائق مرتبط بحسابك");

            if (dto.MaximumCapacity < 1)
                return BadRequest("السعة القصوى يجب أن تكون 1 أو أكثر");

            profile.VehicleType = dto.VehicleType;
            profile.VehiclePlateNumber = dto.VehiclePlateNumber;
            profile.MaximumCapacity = dto.MaximumCapacity;

            await _dbcontext.SaveChangesAsync();

            return Ok(new DriverProfileResponse
            {
                Id = profile.Id,
                VehicleType = profile.VehicleType,
                VehiclePlateNumber = profile.VehiclePlateNumber,
                MaximumCapacity = profile.MaximumCapacity,
                VerificationStatus = profile.VerificationStatus,
                IsAvailable = profile.IsAvailable,
                AverageRating = profile.AverageRating,
                CompletedDeliveries = profile.CompletedDeliveries
            });
        }
        [HttpPost("my/submit-verification")]
        public async Task<IActionResult> SubmitVerification()
        {
            var userId = GetCurrentUserId();

            var profile = await _dbcontext.DriverProfiles.FirstOrDefaultAsync(d => d.UserId == userId);
            if (profile == null)
                return NotFound("لا يوجد بروفايل سائق مرتبط بحسابك");

            if (profile.VerificationStatus != VerificationStatus.Rejected &&
                profile.VerificationStatus != VerificationStatus.NeedsInformation)
                return BadRequest("طلبك أصلاً قيد المراجعة أو تمت الموافقة عليه");

            var hasDocuments = await _dbcontext.VerificationDocuments
                .AnyAsync(d => d.DriverProfileId == profile.Id);
            if (!hasDocuments)
                return BadRequest("لازم ترفع وثيقة تحقق واحدة على الأقل");

            profile.VerificationStatus = VerificationStatus.Pending;

            await _dbcontext.SaveChangesAsync();
            return Ok(new { profile.VerificationStatus });
        }
        [HttpGet("my/verification-status")]
        public async Task<IActionResult> GetVerificationStatus()
        {
            var userId = GetCurrentUserId();

            var profile = await _dbcontext.DriverProfiles.FirstOrDefaultAsync(d => d.UserId == userId);
            if (profile == null)
                return NotFound("لا يوجد بروفايل سائق مرتبط بحسابك");

            return Ok(new { profile.VerificationStatus });
        }

        [HttpPost("my/go-online")]
        public async Task<IActionResult> GoOnline()
        {
            var userId = GetCurrentUserId();

            var profile = await _dbcontext.DriverProfiles.FirstOrDefaultAsync(d => d.UserId == userId);
            if (profile == null)
                return NotFound("لا يوجد بروفايل سائق مرتبط بحسابك");

            if (profile.VerificationStatus != VerificationStatus.Approved)
                return StatusCode(403, "يجب ان يتم توثيق حسابك قبل استقبال المهام");

            if (profile.CurrentLatitude == null || profile.CurrentLongitude == null)
                return BadRequest("يجب تحديث موقعك قبل بدء الوردية");

            profile.IsAvailable = true;

            await _dbcontext.SaveChangesAsync();
            return Ok(new { profile.IsAvailable });
        }
        [HttpPost("my/go-offline")]
        public async Task<IActionResult> GoOffline()
        {
            var userId = GetCurrentUserId();

            var profile = await _dbcontext.DriverProfiles.FirstOrDefaultAsync(d => d.UserId == userId);
            if (profile == null)
                return NotFound("لا يوجد بروفايل سائق مرتبط بحسابك");

            var hasActiveTask = await _dbcontext.DeliveryTasks
                .AnyAsync(t => t.DriverProfileId == profile.Id &&
                               t.Status != DeliveryStatus.Delivered &&
                               t.Status != DeliveryStatus.Failed &&
                               t.Status != DeliveryStatus.Cancelled);

            if (hasActiveTask)
                return BadRequest("لا يمكن إنهاء الوردية، لديك مهمة توصيل قائمة");

            profile.IsAvailable = false;

            await _dbcontext.SaveChangesAsync();
            return Ok(new { profile.IsAvailable });
        }
        [HttpPut("my/location")]
        public async Task<IActionResult> UpdateLocation([FromBody] UpdateLocationRequest dto)
        {
            var userId = GetCurrentUserId();

            var profile = await _dbcontext.DriverProfiles.FirstOrDefaultAsync(d => d.UserId == userId);
            if (profile == null)
                return NotFound("لا يوجد بروفايل سائق مرتبط بحسابك");

            if (dto.Latitude < -90 || dto.Latitude > 90 || dto.Longitude < -180 || dto.Longitude > 180)
                return BadRequest("إحداثيات غير صالحة");

            var now = DateTime.UtcNow;

            profile.CurrentLatitude = dto.Latitude;
            profile.CurrentLongitude = dto.Longitude;
            profile.CurrentLocation = new Point(dto.Longitude, dto.Latitude) { SRID = 4326 };
            profile.LastLocationAt = now;

            var activeTask = await _dbcontext.DeliveryTasks
                .FirstOrDefaultAsync(t => t.DriverProfileId == profile.Id &&
                                          t.Status != DeliveryStatus.Delivered &&
                                          t.Status != DeliveryStatus.Failed &&
                                          t.Status != DeliveryStatus.Cancelled);

            if (activeTask != null)
            {
                _dbcontext.DriverLocations.Add(new DriverLocation
                {
                    Id = Guid.NewGuid(),
                    DeliveryTaskId = activeTask.Id,
                    DriverProfileId = profile.Id,
                    Latitude = dto.Latitude,
                    Longitude = dto.Longitude,
                    RecordedAt = now
                });
            }

            await _dbcontext.SaveChangesAsync();

            if (activeTask != null)
            {
                await _hubContext.Clients
                    .Group(TrackingHub.OrderGroup(activeTask.OrderId))
                    .SendAsync("delivery.location.updated", new
                    {
                        OrderId = activeTask.OrderId,
                        DeliveryTaskId = activeTask.Id,
                        Latitude = dto.Latitude,
                        Longitude = dto.Longitude,
                        RecordedAt = now
                    });
            }

            return Ok(new { profile.CurrentLatitude, profile.CurrentLongitude, profile.LastLocationAt });
        }

        [HttpGet("my/schedule")]
        public async Task<IActionResult> GetMySchedule()
        {
            var userId = GetCurrentUserId();

            var profile = await _dbcontext.DriverProfiles.FirstOrDefaultAsync(d => d.UserId == userId);
            if (profile == null)
                return NotFound("لا يوجد بروفايل سائق مرتبط بحسابك");

            var schedule = await _dbcontext.DriverAvailabilities
                .Where(a => a.DriverProfileId == profile.Id)
                .OrderBy(a => a.DayOfWeek).ThenBy(a => a.StartTime)
                .Select(a => new AvailabilityScheduleResponse
                {
                    Id = a.Id,
                    DayOfWeek = a.DayOfWeek,
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    IsActive = a.IsActive
                })
                .ToListAsync();

            return Ok(schedule);
        }
        [HttpPost("my/schedule")]
        public async Task<IActionResult> AddScheduleSlot([FromBody] CreateAvailabilityScheduleRequest dto)
        {
            var userId = GetCurrentUserId();

            var profile = await _dbcontext.DriverProfiles.FirstOrDefaultAsync(d => d.UserId == userId);
            if (profile == null)
                return NotFound("لا يوجد بروفايل سائق مرتبط بحسابك");

            if (dto.DayOfWeek < 0 || dto.DayOfWeek > 6)
                return BadRequest("يوم الأسبوع يجب أن يكون بين 0 و 6");

            if (dto.StartTime >= dto.EndTime)
                return BadRequest("وقت البداية يجب أن يكون قبل وقت النهاية");

            var overlaps = await _dbcontext.DriverAvailabilities
                .AnyAsync(a => a.DriverProfileId == profile.Id
                            && a.DayOfWeek == dto.DayOfWeek
                            && a.StartTime < dto.EndTime
                            && dto.StartTime < a.EndTime);

            if (overlaps)
                return Conflict("يوجد وردية متداخلة في نفس اليوم");

            var slot = new DriverAvailability
            {
                Id = Guid.NewGuid(),
                DriverProfileId = profile.Id,
                DayOfWeek = dto.DayOfWeek,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                IsActive = true
            };

            _dbcontext.DriverAvailabilities.Add(slot);
            await _dbcontext.SaveChangesAsync();

            return StatusCode(201, new AvailabilityScheduleResponse
            {
                Id = slot.Id,
                DayOfWeek = slot.DayOfWeek,
                StartTime = slot.StartTime,
                EndTime = slot.EndTime,
                IsActive = slot.IsActive
            });
        }
        [HttpPut("my/schedule/{id}")]
        public async Task<IActionResult> UpdateScheduleSlot(Guid id, [FromBody] UpdateAvailabilityScheduleRequest dto)
        {
            var userId = GetCurrentUserId();

            var profile = await _dbcontext.DriverProfiles.FirstOrDefaultAsync(d => d.UserId == userId);
            if (profile == null)
                return NotFound("لا يوجد بروفايل سائق مرتبط بحسابك");

            var slot = await _dbcontext.DriverAvailabilities
                .FirstOrDefaultAsync(a => a.Id == id && a.DriverProfileId == profile.Id);
            if (slot == null)
                return NotFound("الوردية غير موجودة");

            if (dto.StartTime >= dto.EndTime)
                return BadRequest("وقت البداية يجب أن يكون قبل وقت النهاية");

            var overlaps = await _dbcontext.DriverAvailabilities
                .AnyAsync(a => a.DriverProfileId == profile.Id
                            && a.Id != id
                            && a.DayOfWeek == slot.DayOfWeek
                            && a.StartTime < dto.EndTime
                            && dto.StartTime < a.EndTime);

            if (overlaps)
                return Conflict("يوجد وردية متداخلة في نفس اليوم");

            slot.StartTime = dto.StartTime;
            slot.EndTime = dto.EndTime;
            slot.IsActive = dto.IsActive;

            await _dbcontext.SaveChangesAsync();

            return Ok(new AvailabilityScheduleResponse
            {
                Id = slot.Id,
                DayOfWeek = slot.DayOfWeek,
                StartTime = slot.StartTime,
                EndTime = slot.EndTime,
                IsActive = slot.IsActive
            });
        }
        [HttpDelete("my/schedule/{id}")]
        public async Task<IActionResult> DeleteScheduleSlot(Guid id)
        {
            var userId = GetCurrentUserId();

            var profile = await _dbcontext.DriverProfiles.FirstOrDefaultAsync(d => d.UserId == userId);
            if (profile == null)
                return NotFound("لا يوجد بروفايل سائق مرتبط بحسابك");

            var slot = await _dbcontext.DriverAvailabilities
                .FirstOrDefaultAsync(a => a.Id == id && a.DriverProfileId == profile.Id);
            if (slot == null)
                return NotFound("الوردية غير موجودة");

            _dbcontext.DriverAvailabilities.Remove(slot);
            await _dbcontext.SaveChangesAsync();
            return NoContent();
        }
        [HttpGet("my/stats")]
        public async Task<IActionResult> GetMyStats()
        {
            var userId = GetCurrentUserId();

            var profile = await _dbcontext.DriverProfiles.FirstOrDefaultAsync(d => d.UserId == userId);
            if (profile == null)
                return NotFound("لا يوجد بروفايل سائق مرتبط بحسابك");

            var totalTasks = await _dbcontext.DeliveryTasks
                .CountAsync(t => t.DriverProfileId == profile.Id);

            var deliveredTasks = await _dbcontext.DeliveryTasks
                .CountAsync(t => t.DriverProfileId == profile.Id && t.Status == DeliveryStatus.Delivered);

            var failedTasks = await _dbcontext.DeliveryTasks
                .CountAsync(t => t.DriverProfileId == profile.Id && t.Status == DeliveryStatus.Failed);

            return Ok(new
            {
                profile.CompletedDeliveries,
                profile.AverageRating,
                profile.IsAvailable,
                TotalTasks = totalTasks,
                DeliveredTasks = deliveredTasks,
                FailedTasks = failedTasks
            });
        }
        private Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}

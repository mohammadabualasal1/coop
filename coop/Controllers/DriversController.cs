using coop.Dtos.DriversController;
using coop.Dtos.DriversDtos;
using coop.Enums;
using coop.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace coop.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = "Driver")]
    [ApiController]
    public class DriversController : ControllerBase
    {
        private CoopDbContext _dbcontext;

        public DriversController(CoopDbContext dbcontext)
        {
            _dbcontext = dbcontext;
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

        private Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}

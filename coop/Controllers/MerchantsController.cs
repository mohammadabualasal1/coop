using coop.Dtos.MerchantsController;
using coop.Enums;
using coop.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace coop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MerchantsController : ControllerBase
    {
        private readonly CoopDbContext _dbcontext;

        public MerchantsController(CoopDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }
        [HttpPost("AddMerchant")]
        public async Task<IActionResult> AddMerchant([FromBody] CreateMerchantRequestDto dto)
        {
            var userId = GetCurrentUserId();
            var existingMerchant = await _dbcontext.Merchants.AnyAsync(m => m.OwnerUserId == userId);
            if (existingMerchant)
            {
                return BadRequest("لديك بروفايل تاجر بالفعل");
            }
            var newMerchant = new Merchant
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description,
                OwnerUserId = userId,
                CreatedAt = DateTime.UtcNow,
                ContactEmail = dto.ContactEmail,
                ContactPhone = dto.ContactPhone,
                CoverImageUrl = dto.CoverImageUrl,
                LogoUrl = dto.LogoUrl,
                VerificationStatus = VerificationStatus.Pending,
                IsActive = true,
                RegistrationNumber = dto.RegistrationNumber,



            };
            _dbcontext.Merchants.Add(newMerchant);
            await _dbcontext.SaveChangesAsync();
            return Ok(newMerchant);
        }

        private Guid GetCurrentUserId() =>
          Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}

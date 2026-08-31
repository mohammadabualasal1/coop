using coop.Dtos.MerchantBranchesController;
using coop.Dtos.MerchantsController;
using coop.Enums;
using coop.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
namespace coop.Controllers
{
    [Route("api/merchants")]
    [ApiController]
    [Authorize(Roles = "Merchant")]
    public class MerchantsController : ControllerBase
    {
        private readonly CoopDbContext _dbcontext;

        public MerchantsController(CoopDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }
      
        [HttpGet("my")]
        public async Task<IActionResult> GetMyMerchant()
        {
            var userId = GetCurrentUserId();
            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
            if (merchant == null)
                return NotFound("لا يوجد بروفايل تاجر مرتبط بحسابك");
            return Ok(merchant);
        }
        [HttpPut("my")]
        public async Task<IActionResult> UpdateMyMerchant([FromBody] UpdateMerchantRequestDto dto)
        {
            var userId = GetCurrentUserId();
          var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
            if (merchant == null)
                return NotFound("لا يوجد بروفايل تاجر مرتبط بحسابك");


            merchant.Name = dto.Name;
            merchant.Description = dto.Description;
            merchant.ContactEmail = dto.ContactEmail;
            merchant.ContactPhone = dto.ContactPhone;
            merchant.CoverImageUrl = dto.CoverImageUrl;
            merchant.LogoUrl = dto.LogoUrl;
            await _dbcontext.SaveChangesAsync();
            return Ok(merchant);

        }

       

        [HttpGet("my/verification-status")]
        public async Task<IActionResult> GetVerificationStatus()
        {
           var userId = GetCurrentUserId();
            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
            if (merchant == null)
                return NotFound("لا يوجد بروفايل تاجر مرتبط بحسابك");
            return Ok(new VerificationStatusResponseDto
            {
                VerificationStatus = merchant.VerificationStatus,
                RejectionReason = merchant.RejectionReason,
                VerifiedAt = merchant.VerifiedAt
            });
        }

       

        private Guid GetCurrentUserId() =>
          Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}

using System.Security.Claims;
using coop.Dtos.VerificationDocumentsController;
using coop.Enums;
using coop.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace coop.Controllers
{
    [ApiController]
    [Route("api/verification-documents")]
    public class VerificationDocumentsController : ControllerBase
    {
        private readonly CoopDbContext _dbcontext;

        public VerificationDocumentsController(CoopDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }

        [Authorize(Roles = "Merchant")]
        [HttpPost]
        public async Task<ActionResult<VerificationDocumentResponseDto>> Upload(UploadVerificationDocumentRequestDto dto)
        {
            var userId = GetCurrentUserId();

            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
            if (merchant == null)
            {
                return BadRequest("لا يوجد حساب تاجر مرتبط بهذا المستخدم.");
            }
            var document = new VerificationDocument
            {
                Id = Guid.NewGuid(),
                MerchantId = merchant.Id,
                DocumentType = dto.DocumentType,
                FileUrl = dto.FileUrl,
                Status = VerificationStatus.Pending,
                UploadedAt = DateTime.UtcNow
            };

            _dbcontext.VerificationDocuments.Add(document);
            await _dbcontext.SaveChangesAsync();

            var response = new VerificationDocumentResponseDto
            {
                Id = document.Id,
                DocumentType = document.DocumentType,
                FileUrl = document.FileUrl,
                Status = document.Status,
                ReviewNote = document.ReviewNote,
                UploadedAt = document.UploadedAt,
                ReviewedAt = document.ReviewedAt
            };

            return Ok(response);
        }

        private Guid GetCurrentUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
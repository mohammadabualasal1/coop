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
        private CoopDbContext _dbcontext;

        public VerificationDocumentsController(CoopDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }

        [Authorize(Roles = "Merchant")]
        [HttpPost]
        public async Task<ActionResult<VerificationDocumentResponseDto>> Upload(UploadVerificationDocumentRequestDto dto)
        {
            var userId = GetCurrentUserId();    // token

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

        [Authorize(Roles = "Merchant")]
        [HttpGet("my")]
        public async Task<ActionResult<List<VerificationDocumentResponseDto>>> GetMyDocuments()
        {
            var userId = GetCurrentUserId();

            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
            if (merchant == null)
            {
                return BadRequest("لا يوجد حساب تاجر مرتبط بهذا المستخدم.");
            }
            var documents = await _dbcontext.VerificationDocuments
                           .Where(d => d.MerchantId == merchant.Id)
                           .OrderBy(d => d.UploadedAt)
                           .Select(d => new VerificationDocumentResponseDto
                           {
                           Id = d.Id,
                           DocumentType = d.DocumentType,
                           FileUrl = d.FileUrl,
                           Status = d.Status,
                           ReviewNote = d.ReviewNote,
                           UploadedAt = d.UploadedAt,
                           ReviewedAt = d.ReviewedAt
                          }).ToListAsync();

            return Ok(documents);
        }
        [Authorize(Roles = "Merchant")]
        [HttpGet("{id}")]
        public async Task<ActionResult<VerificationDocumentResponseDto>> GetById(Guid id)
        {
            var userId = GetCurrentUserId();

            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
            if (merchant == null)
            {
                return BadRequest("لا يوجد حساب تاجر مرتبط بهذا المستخدم.");
            }
            var document = await _dbcontext.VerificationDocuments.FirstOrDefaultAsync(d => d.Id == id && d.MerchantId == merchant.Id);

            if (document == null)
            {
                return NotFound();
            }
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
        [Authorize(Roles = "Merchant")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = GetCurrentUserId();

            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
            if (merchant == null)
            {
                return BadRequest("لا يوجد حساب تاجر مرتبط بهذا المستخدم.");
            }

            var document = await _dbcontext.VerificationDocuments
                .FirstOrDefaultAsync(d => d.Id == id && d.MerchantId == merchant.Id);

            if (document == null)
            {
                return NotFound();
            }

            if (document.Status != VerificationStatus.Pending)
            {
                return BadRequest("لا يمكن حذف الوثيقة بعد مراجعتها من قبل الأدمن.");
            }

            _dbcontext.VerificationDocuments.Remove(document);
            await _dbcontext.SaveChangesAsync();

            return NoContent();
        }

        private Guid GetCurrentUserId() =>
           Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
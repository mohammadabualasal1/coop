using coop.Dtos.ProductsController;
using coop.Enums;
using coop.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace coop.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = "Merchant")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly CoopDbContext _dbcontext;

        public ProductsController(CoopDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }
        [HttpPost]
        public async Task<IActionResult> AddProduct([FromBody] CreateProductRequest dto)
        {
            var userId = GetCurrentUserId();
            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
            if (merchant == null)
                return NotFound("لا يوجد بروفايل تاجر مرتبط بحسابك");

            if (merchant.VerificationStatus != VerificationStatus.Approved)
                return StatusCode(403, "يجب ان يتم توثيق حسابك قبل إضافة منتجات");

            var categoryExists = await _dbcontext.Categories.AnyAsync(c => c.Id == dto.CategoryId && c.IsActive);
            if (!categoryExists)
                return BadRequest("الفئة غير موجودة");

            var newProduct = new Product
            {
                Id = Guid.NewGuid(),
                MerchantId = merchant.Id,
                CategoryId = dto.CategoryId,
                Name = dto.Name,
                Description = dto.Description,
                Sku = dto.Sku,
                Brand = dto.Brand,
                MainImageUrl = dto.MainImageUrl,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true,
            };

            _dbcontext.Products.Add(newProduct);
            await _dbcontext.SaveChangesAsync();
            return Ok(newProduct);
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyProducts()
        {
            var userId = GetCurrentUserId();
            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
            if (merchant == null)
                return NotFound("لا يوجد بروفايل تاجر مرتبط بحسابك");

            var products = await _dbcontext.Products
                .Where(p => p.MerchantId == merchant.Id && p.IsActive)
                .ToListAsync();

            return Ok(products);
        }
        private Guid GetCurrentUserId() =>
          Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    }
}

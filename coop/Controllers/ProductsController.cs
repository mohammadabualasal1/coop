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
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(Guid id)
        {
            var userId = GetCurrentUserId();
            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
            if (merchant == null)
                return NotFound("لا يوجد بروفايل تاجر مرتبط بحسابك");

            var product = await _dbcontext.Products.FirstOrDefaultAsync(p => p.Id == id && p.MerchantId == merchant.Id);
            if (product == null)
                return NotFound("المنتج غير موجود");

            var images = await _dbcontext.ProductImages
                .Where(i => i.ProductId == id)
                .OrderBy(i => i.DisplayOrder)
                .Select(i => new ProductImageResponseDto
                {
                    Id = i.Id,
                    FileUrl = i.FileUrl,
                    DisplayOrder = i.DisplayOrder
                })
                .ToListAsync();

            return Ok(new ProductResponse
            {
                Id = product.Id,
                MerchantId = product.MerchantId,
                CategoryId = product.CategoryId,
                Name = product.Name,
                Description = product.Description,
                Sku = product.Sku,
                Brand = product.Brand,
                MainImageUrl = product.MainImageUrl,
                IsActive = product.IsActive,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt,
                Images = images
            });
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductRequestDto dto)
        {
            var userId = GetCurrentUserId();
            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
            if (merchant == null)
                return NotFound("لا يوجد بروفايل تاجر مرتبط بحسابك");

            var product = await _dbcontext.Products.FirstOrDefaultAsync(p => p.Id == id && p.MerchantId == merchant.Id);
            if (product == null)
                return NotFound("المنتج غير موجود");

            var categoryExists = await _dbcontext.Categories.AnyAsync(c => c.Id == dto.CategoryId && c.IsActive);
            if (!categoryExists)
                return BadRequest("الفئة غير موجودة");

            product.CategoryId = dto.CategoryId;
            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Sku = dto.Sku;
            product.Brand = dto.Brand;
            product.MainImageUrl = dto.MainImageUrl;
            product.IsActive = dto.IsActive;
            product.UpdatedAt = DateTime.UtcNow;

            await _dbcontext.SaveChangesAsync();
            return Ok(product);
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeactivateProduct(Guid id)
        {
            var userId = GetCurrentUserId();
            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
            if (merchant == null)
                return NotFound("لا يوجد بروفايل تاجر مرتبط بحسابك");

            var product = await _dbcontext.Products.FirstOrDefaultAsync(p => p.Id == id && p.MerchantId == merchant.Id);
            if (product == null)
                return NotFound("المنتج غير موجود");

            product.IsActive = false;
            product.UpdatedAt = DateTime.UtcNow;

            await _dbcontext.SaveChangesAsync();
            return NoContent();
        }


        [HttpPost("{id}/images")]
        public async Task<IActionResult> AddProductImage(Guid id, [FromBody] UploadProductImageRequestDto dto)
        {
            var userId = GetCurrentUserId();
            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
            if (merchant == null)
                return NotFound("لا يوجد بروفايل تاجر مرتبط بحسابك");

            var product = await _dbcontext.Products.FirstOrDefaultAsync(p => p.Id == id && p.MerchantId == merchant.Id);
            if (product == null)
                return NotFound("المنتج غير موجود");

            var newImage = new ProductImage
            {
                Id = Guid.NewGuid(),
                ProductId = id,
                FileUrl = dto.FileUrl,
                DisplayOrder = dto.DisplayOrder,
                CreatedAt = DateTime.UtcNow
            };

            _dbcontext.ProductImages.Add(newImage);
            await _dbcontext.SaveChangesAsync();

            return Ok(new ProductImageResponseDto
            {
                Id = newImage.Id,
                FileUrl = newImage.FileUrl,
                DisplayOrder = newImage.DisplayOrder
            });
        }
        [HttpDelete("{id}/images/{imageId}")]
        public async Task<IActionResult> DeleteProductImage(Guid id, Guid imageId)
        {
            var userId = GetCurrentUserId();
            var merchant = await _dbcontext.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
            if (merchant == null)
                return NotFound("لا يوجد بروفايل تاجر مرتبط بحسابك");

            var product = await _dbcontext.Products.FirstOrDefaultAsync(p => p.Id == id && p.MerchantId == merchant.Id);
            if (product == null)
                return NotFound("المنتج غير موجود");

            var image = await _dbcontext.ProductImages.FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == id);
            if (image == null)
                return NotFound("الصورة غير موجودة");

            _dbcontext.ProductImages.Remove(image);
            await _dbcontext.SaveChangesAsync();
            return NoContent();
        }
    

        private Guid GetCurrentUserId() =>
          Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    }
}

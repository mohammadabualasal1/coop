using coop.Dtos.CategoriesController;
using coop.Dtos.MarketplaceController;
using coop.Enums;
using coop.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace coop.Controllers
{
    [ApiController]
    [Route("api/categories")]
    public class CategoriesController : ControllerBase
    {
        private CoopDbContext _dbcontext;

        public CategoriesController(CoopDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }

        [HttpGet]
        public async Task<IActionResult> GetCategoryTree()
        {
            var categories = await _dbcontext.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new CategoryResponse
                {
                    Id = c.Id,
                    ParentCategoryId = c.ParentCategoryId,
                    NameEn = c.NameEn,
                    NameAr = c.NameAr,
                    ImageUrl = c.ImageUrl,
                    DisplayOrder = c.DisplayOrder,
                    Children = new List<CategoryResponse>()
                })
                .ToListAsync();

            var lookup = categories.ToDictionary(c => c.Id);
            var rootCategories = new List<CategoryResponse>();

            foreach (var category in categories)
            {
                if (category.ParentCategoryId == null)
                {
                    rootCategories.Add(category);
                }
                else if (lookup.ContainsKey(category.ParentCategoryId.Value))
                {
                    lookup[category.ParentCategoryId.Value].Children.Add(category);
                }
            }
            return Ok(rootCategories);
        }
        [HttpGet("{id}/offers")]
        public async Task<IActionResult> GetCategoryOffers(Guid id)
        {
            var categoryExists = await _dbcontext.Categories.AnyAsync(c => c.Id == id && c.IsActive);
            if (!categoryExists)
                return NotFound("الفئة غير موجودة");

            var now = DateTime.UtcNow;

            var offers = await _dbcontext.Offers
                .Where(o => o.Product.CategoryId == id
                         && o.Status == OfferStatus.Active
                         && o.StartAt <= now
                         && o.EndAt >= now
                         && _dbcontext.BranchOffers.Any(bo => bo.OfferId == o.Id
                                                           && bo.IsAvailable
                                                           && bo.TotalStock - bo.ReservedStock - bo.SoldStock > 0))
                .OrderByDescending(o => o.DiscountPercentage)
                .Select(o => new OfferSummaryResponse
                {
                    Id = o.Id,
                    Title = o.Title,
                    MerchantId = o.MerchantId,
                    MerchantName = o.Merchant.Name,
                    MainImageUrl = o.Product.MainImageUrl,
                    OriginalPrice = o.OriginalPrice,
                    DiscountedPrice = o.DiscountedPrice,
                    DiscountPercentage = o.DiscountPercentage,
                    EndAt = o.EndAt,
                    DistanceKm = null
                })
                .ToListAsync();

            return Ok(offers);
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateCategory(CreateCategoryRequestDto dto)
        {
            if (dto.ParentCategoryId != null)
            {
                var parentExists = await _dbcontext.Categories.AnyAsync(c => c.Id == dto.ParentCategoryId);
                if (!parentExists)
                    return BadRequest("الفئة الرئيسية غير موجودة");
            }

            var category = new Category
            {
                Id = Guid.NewGuid(),
                ParentCategoryId = dto.ParentCategoryId,
                NameEn = dto.NameEn,
                NameAr = dto.NameAr,
                Description = dto.Description,
                ImageUrl = dto.ImageUrl,
                DisplayOrder = dto.DisplayOrder,
                IsActive = true
            };

            _dbcontext.Categories.Add(category);
            await _dbcontext.SaveChangesAsync();

            return Ok(category);
        }
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(Guid id, UpdateCategoryRequestDto dto)
        {
            var category = await _dbcontext.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
                return NotFound("الفئة غير موجودة");

            if (dto.ParentCategoryId == id)
                return BadRequest("لا يمكن أن تكون الفئة الرئيسية رئيسة لنفسها");

            if (dto.ParentCategoryId != null)
            {
                var parentExists = await _dbcontext.Categories.AnyAsync(c => c.Id == dto.ParentCategoryId);
                if (!parentExists)
                    return BadRequest("الفئة الرئيسية غير موجودة");
            }

            category.ParentCategoryId = dto.ParentCategoryId;
            category.NameEn = dto.NameEn;
            category.NameAr = dto.NameAr;
            category.Description = dto.Description;
            category.ImageUrl = dto.ImageUrl;
            category.DisplayOrder = dto.DisplayOrder;

            await _dbcontext.SaveChangesAsync();

            return Ok(category);
        }
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            var category = await _dbcontext.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
                return NotFound("الفئة غير موجودة");

            var hasActiveChildren = await _dbcontext.Categories
                .AnyAsync(c => c.ParentCategoryId == id && c.IsActive);

            if (hasActiveChildren)
                return BadRequest("لا يمكن حذف فئة تحتوي على فئات فرعية نشطة");

            category.IsActive = false;
            await _dbcontext.SaveChangesAsync();

            return NoContent();
        }
    }
}
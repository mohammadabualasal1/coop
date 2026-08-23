using coop.Dtos.CategoriesController;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace coop.Controllers
{
    [ApiController]
    [Route("api/categories")]
    public class CategoriesController : ControllerBase
    {
        private readonly CoopDbContext _dbcontext;

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
    }
}
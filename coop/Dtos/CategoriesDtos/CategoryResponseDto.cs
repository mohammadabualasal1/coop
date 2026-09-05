using System.Collections.Generic;
namespace coop.Dtos.CategoriesController
{
    public class CategoryResponse
    {
        public Guid Id { get; set; }
        public Guid? ParentCategoryId { get; set; }
        public string NameEn { get; set; }
        public string NameAr { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public List<CategoryResponse> Children { get; set; }
    }
}

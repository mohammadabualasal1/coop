using System.ComponentModel.DataAnnotations.Schema;

namespace coop.Model
{
    public class Category
    {
        public Guid Id { get; set; }

        [ForeignKey("ParentCategoryId")]
        public Guid? ParentCategoryId { get; set; }
        public Category? ParentCategory { get; set; }

        public string NameEn { get; set; }
        public string NameAr { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
    }
}

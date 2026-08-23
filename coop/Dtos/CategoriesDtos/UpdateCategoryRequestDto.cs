namespace coop.Dtos.CategoriesController
{
    public class UpdateCategoryRequestDto
    {
        public Guid? ParentCategoryId { get; set; }
        public string NameEn { get; set; }
        public string NameAr { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public int DisplayOrder { get; set; }
    }
}
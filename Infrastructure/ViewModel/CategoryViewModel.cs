namespace Infrastructure.ViewModel
{
    public class CategoryViewModel
    {
        public List<CategoryDto> Categories { get; set; } = new();
        public List<LogCategoryDto> LogCategories { get; set; } = new();
        public NewCategory NewCategory { get; set; } = new();
    }
}

namespace Infrastructure.ViewModel
{
    /// <summary>Scalar card shown to everyone on the public home page.</summary>
    public sealed class AvailableBookCardDto
    {
        public Guid BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? AuthorName { get; set; }
        public string? CategoryName { get; set; }
        public string? ImageThumbnailUrl { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsAvailableForRental { get; set; }
    }

    /// <summary>Read-only model for the public home page.</summary>
    public sealed class PublicHomeViewModel
    {
        public List<AvailableBookCardDto> AvailableBooks { get; set; } = new();
    }
}

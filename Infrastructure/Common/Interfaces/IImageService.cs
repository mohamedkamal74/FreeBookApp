using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Common.Interfaces
{
    public class ImageUploadResult
    {
        public bool Success { get; set; }
        public string PublicId { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
    }

    public interface IImageService
    {
        Task<ImageUploadResult> UploadAsync(IFormFile file, string folder = "books");
        Task<ImageUploadResult> UploadAsync(System.IO.Stream stream, string fileName, string folder = "books");
        Task DeleteAsync(string publicId);
        string GetImageUrl(string publicId, int? width = null, int? height = null);
        string GetThumbnailUrl(string publicId, int width = 200, int height = 200);
    }
}

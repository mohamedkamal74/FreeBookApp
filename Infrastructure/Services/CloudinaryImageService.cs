using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Infrastructure.Common.Configuration;
using Infrastructure.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services
{
    public class CloudinaryImageService : IImageService
    {
        private readonly Cloudinary _cloudinary;
        private readonly CloudinarySettings _settings;
        private readonly ILogger<CloudinaryImageService> _logger;

        public CloudinaryImageService(IOptions<CloudinarySettings> settings, ILogger<CloudinaryImageService> logger)
        {
            _settings = settings.Value;
            _logger = logger;

            var account = new Account(_settings.CloudName, _settings.ApiKey, _settings.ApiSecret);
            _cloudinary = new Cloudinary(account);
        }

        public async Task<Infrastructure.Common.Interfaces.ImageUploadResult> UploadAsync(IFormFile file, string folder = "books")
        {
            if (file == null || file.Length == 0)
                return new Infrastructure.Common.Interfaces.ImageUploadResult { Success = false, Error = "No file provided" };

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            if (!allowedExtensions.Contains(ext))
                return new Infrastructure.Common.Interfaces.ImageUploadResult { Success = false, Error = "Invalid file type. Allowed: jpg, jpeg, png, gif, webp" };

            var maxSizeBytes = 10 * 1024 * 1024; // 10 MB
            if (file.Length > maxSizeBytes)
                return new Infrastructure.Common.Interfaces.ImageUploadResult { Success = false, Error = "File too large. Max 10 MB" };

            try
            {
                using var stream = file.OpenReadStream();
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = $"{_settings.Folder}/{folder}",
                    Transformation = new Transformation().Quality("auto").FetchFormat("auto")
                };

                var result = await _cloudinary.UploadAsync(uploadParams);

                return new Infrastructure.Common.Interfaces.ImageUploadResult
                {
                    Success = result.Error == null,
                    PublicId = result.PublicId,
                    Url = result.SecureUrl?.AbsoluteUri ?? string.Empty,
                    ThumbnailUrl = _cloudinary.Api.UrlImgUp
                        .Transform(new Transformation().Width(200).Height(200).Crop("fill"))
                        .BuildUrl(result.PublicId),
                    Error = result.Error?.Message,
                    FileSize = file.Length,
                    ContentType = file.ContentType
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload image to Cloudinary");
                return new Infrastructure.Common.Interfaces.ImageUploadResult { Success = false, Error = ex.Message };
            }
        }

        public async Task<Infrastructure.Common.Interfaces.ImageUploadResult> UploadAsync(Stream stream, string fileName, string folder = "books")
        {
            if (stream == null || stream.Length == 0)
                return new Infrastructure.Common.Interfaces.ImageUploadResult { Success = false, Error = "Empty stream" };

            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            if (!allowedExtensions.Contains(ext))
                return new Infrastructure.Common.Interfaces.ImageUploadResult { Success = false, Error = "Invalid file type" };

            try
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(fileName, stream),
                    Folder = $"{_settings.Folder}/{folder}",
                    Transformation = new Transformation().Quality("auto").FetchFormat("auto")
                };

                var result = await _cloudinary.UploadAsync(uploadParams);

                return new Infrastructure.Common.Interfaces.ImageUploadResult
                {
                    Success = result.Error == null,
                    PublicId = result.PublicId,
                    Url = result.SecureUrl?.AbsoluteUri ?? string.Empty,
                    ThumbnailUrl = _cloudinary.Api.UrlImgUp
                        .Transform(new Transformation().Width(200).Height(200).Crop("fill"))
                        .BuildUrl(result.PublicId),
                    Error = result.Error?.Message,
                    FileSize = stream.Length,
                    ContentType = GetContentType(ext)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload image stream to Cloudinary");
                return new Infrastructure.Common.Interfaces.ImageUploadResult { Success = false, Error = ex.Message };
            }
        }

        public async Task DeleteAsync(string publicId)
        {
            if (string.IsNullOrEmpty(publicId))
                return;

            try
            {
                await _cloudinary.DeleteResourcesAsync(new[] { publicId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete image from Cloudinary: {PublicId}", publicId);
            }
        }

        public string GetImageUrl(string publicId, int? width = null, int? height = null)
        {
            if (string.IsNullOrEmpty(publicId))
                return string.Empty;

            var transformation = new Transformation();
            if (width.HasValue) transformation = transformation.Width(width.Value);
            if (height.HasValue) transformation = transformation.Height(height.Value);
            if (width.HasValue || height.HasValue) transformation = transformation.Crop("fill");

            return _cloudinary.Api.UrlImgUp.Transform(transformation).BuildUrl(publicId);
        }

        public string GetThumbnailUrl(string publicId, int width = 200, int height = 200)
        {
            return GetImageUrl(publicId, width, height);
        }

        private string GetContentType(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
        }
    }
}

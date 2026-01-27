using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Monolith.WebAPI.Services.Storage
{
    public interface ICloudinaryService
    {
        Task<CloudinaryUploadResult> UploadAsync(Stream stream, string folder, string publicId = null, string resourceType = "image");
        string GetOptimizedUrl(string publicId, CloudinaryTransformation transformation);
        string GetThumbnailUrl(string publicId, int width = 200, int height = 200);
        string GetWhatsAppUrl(string publicId);
        Task<bool> DeleteAsync(string publicId);
    }

    public class CloudinaryUploadResult
    {
        public bool Success { get; set; }
        public string PublicId { get; set; }
        public string Url { get; set; }
        public string SecureUrl { get; set; }
        public string Format { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public long Size { get; set; }
        public string Error { get; set; }
    }

    public class CloudinaryTransformation
    {
        public int? Width { get; set; }
        public int? Height { get; set; }
        public string Crop { get; set; } = "fill";
        public string Quality { get; set; } = "auto";
        public string Format { get; set; } = "auto";
        public string Gravity { get; set; } = "center";
    }

    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<CloudinaryService> _logger;
        private readonly IConfiguration _configuration;
        private readonly bool _useCloudinary;

        public CloudinaryService(IConfiguration configuration, ILogger<CloudinaryService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            
            // Cloudinary kullanımı config'den kontrol edilir
            _useCloudinary = configuration.GetValue<bool>("Cloudinary:Enabled");
            
            if (_useCloudinary)
            {
                var cloudName = configuration["Cloudinary:CloudName"];
                var apiKey = configuration["Cloudinary:ApiKey"];
                var apiSecret = configuration["Cloudinary:ApiSecret"];

                if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
                {
                    _logger.LogWarning("Cloudinary credentials not configured properly");
                    _useCloudinary = false;
                    return;
                }

                var account = new Account(cloudName, apiKey, apiSecret);
                _cloudinary = new Cloudinary(account);
                _cloudinary.Api.Secure = true; // Always use HTTPS
            }
        }

        public async Task<CloudinaryUploadResult> UploadAsync(Stream stream, string folder, string publicId = null, string resourceType = "image")
        {
            if (!_useCloudinary)
            {
                return new CloudinaryUploadResult 
                { 
                    Success = false, 
                    Error = "Cloudinary is not enabled" 
                };
            }

            try
            {
                // Generate unique public ID if not provided
                if (string.IsNullOrEmpty(publicId))
                {
                    publicId = $"{folder}/{Guid.NewGuid():N}";
                }
                else if (!publicId.StartsWith(folder))
                {
                    publicId = $"{folder}/{publicId}";
                }

                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(publicId, stream),
                    PublicId = publicId,
                    Folder = folder,
                    UseFilename = false,
                    UniqueFilename = false,
                    Overwrite = true,
                    // Otomatik optimizasyon
                    Transformation = new Transformation()
                        .Quality("auto")
                        .FetchFormat("auto"),
                    // Backup ve analiz
                    Backup = true,
                    Colors = true,
                    Faces = true,
                    QualityAnalysis = true
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.Error != null)
                {
                    _logger.LogError($"Cloudinary upload error: {uploadResult.Error.Message}");
                    return new CloudinaryUploadResult
                    {
                        Success = false,
                        Error = uploadResult.Error.Message
                    };
                }

                _logger.LogInformation($"File uploaded to Cloudinary: {uploadResult.SecureUrl}");

                return new CloudinaryUploadResult
                {
                    Success = true,
                    PublicId = uploadResult.PublicId,
                    Url = uploadResult.Url.ToString(),
                    SecureUrl = uploadResult.SecureUrl.ToString(),
                    Format = uploadResult.Format,
                    Width = uploadResult.Width,
                    Height = uploadResult.Height,
                    Size = uploadResult.Bytes
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading to Cloudinary");
                return new CloudinaryUploadResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public string GetOptimizedUrl(string publicId, CloudinaryTransformation transformation)
        {
            if (!_useCloudinary || string.IsNullOrEmpty(publicId))
                return publicId;

            try
            {
                var trans = new Transformation();
                
                if (transformation.Width.HasValue)
                    trans = trans.Width(transformation.Width.Value);
                    
                if (transformation.Height.HasValue)
                    trans = trans.Height(transformation.Height.Value);
                    
                if (!string.IsNullOrEmpty(transformation.Crop))
                    trans = trans.Crop(transformation.Crop);
                    
                if (!string.IsNullOrEmpty(transformation.Quality))
                    trans = trans.Quality(transformation.Quality);
                    
                if (!string.IsNullOrEmpty(transformation.Format))
                    trans = trans.FetchFormat(transformation.Format);
                    
                if (!string.IsNullOrEmpty(transformation.Gravity))
                    trans = trans.Gravity(transformation.Gravity);

                return _cloudinary.Api.UrlImgUp
                    .Transform(trans)
                    .Secure(true)
                    .BuildUrl(publicId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating optimized URL for {publicId}");
                return publicId;
            }
        }

        public string GetThumbnailUrl(string publicId, int width = 200, int height = 200)
        {
            return GetOptimizedUrl(publicId, new CloudinaryTransformation
            {
                Width = width,
                Height = height,
                Crop = "thumb",
                Gravity = "center",
                Quality = "auto"
            });
        }

        public string GetWhatsAppUrl(string publicId)
        {
            // WhatsApp için optimize edilmiş URL
            // Maksimum 300x300, JPEG, %80 kalite
            return GetOptimizedUrl(publicId, new CloudinaryTransformation
            {
                Width = 300,
                Height = 300,
                Crop = "limit", // Aspect ratio'yu koru
                Quality = "80",
                Format = "jpg"
            });
        }

        public async Task<bool> DeleteAsync(string publicId)
        {
            if (!_useCloudinary || string.IsNullOrEmpty(publicId))
                return false;

            try
            {
                var deleteParams = new DeletionParams(publicId)
                {
                    ResourceType = ResourceType.Image
                };

                var result = await _cloudinary.DestroyAsync(deleteParams);
                
                if (result.Error != null)
                {
                    _logger.LogError($"Cloudinary delete error: {result.Error.Message}");
                    return false;
                }

                _logger.LogInformation($"File deleted from Cloudinary: {publicId}");
                return result.Result == "ok";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting from Cloudinary: {publicId}");
                return false;
            }
        }
    }
}
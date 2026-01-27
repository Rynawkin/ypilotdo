namespace Monolith.WebAPI.Services.Storage
{
    // Cloudinary devre dışı olduğunda kullanılacak
    public class NullCloudinaryService : ICloudinaryService
    {
        public Task<CloudinaryUploadResult> UploadAsync(Stream stream, string folder, string publicId = null, string resourceType = "image")
        {
            return Task.FromResult(new CloudinaryUploadResult 
            { 
                Success = false, 
                Error = "Cloudinary is not configured" 
            });
        }

        public string GetOptimizedUrl(string publicId, CloudinaryTransformation transformation)
        {
            return publicId;
        }

        public string GetThumbnailUrl(string publicId, int width = 200, int height = 200)
        {
            return publicId;
        }

        public string GetWhatsAppUrl(string publicId)
        {
            return publicId;
        }

        public Task<bool> DeleteAsync(string publicId)
        {
            return Task.FromResult(false);
        }
    }
}
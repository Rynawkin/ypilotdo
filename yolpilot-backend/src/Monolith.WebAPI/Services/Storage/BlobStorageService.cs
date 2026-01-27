// src/Monolith.WebAPI/Services/Storage/BlobStorageService.cs
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Monolith.WebAPI.Services.Storage
{
    public interface IBlobStorageService
    {
        Task<string> UploadAsync(Stream fileStream, string fileName, string contentType);
        Task<bool> DeleteAsync(string fileUrl);
        Task<Stream> DownloadAsync(string fileUrl);
    }

    public class BlobStorageService : IBlobStorageService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<BlobStorageService> _logger;
        private readonly string _localStoragePath;
        private readonly bool _useLocalStorage;

        public BlobStorageService(IConfiguration configuration, ILogger<BlobStorageService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _useLocalStorage = configuration.GetValue<bool>("Azure:Storage:UseLocalStorage");
            _localStoragePath = Path.Combine(Directory.GetCurrentDirectory(), 
                configuration["Azure:Storage:LocalStoragePath"] ?? "wwwroot/uploads");
            
            if (_useLocalStorage)
            {
                Directory.CreateDirectory(_localStoragePath);
            }
        }

        public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType)
        {
            if (_useLocalStorage)
            {
                // Local storage implementation for development
                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
                var filePath = Path.Combine(_localStoragePath, uniqueFileName);
                
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                
                using (var fileOutput = File.Create(filePath))
                {
                    await fileStream.CopyToAsync(fileOutput);
                }
                
                // Return relative URL
                return $"/uploads/{uniqueFileName}";
            }
            else
            {
                // Azure Blob Storage implementation
                var connectionString = _configuration["Azure:Storage:ConnectionString"];
                var containerName = _configuration["Azure:Storage:ContainerName"];
                
                var blobServiceClient = new BlobServiceClient(connectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
                
                var blobName = $"{Guid.NewGuid()}/{fileName}";
                var blobClient = containerClient.GetBlobClient(blobName);
                
                await blobClient.UploadAsync(fileStream, new BlobHttpHeaders { ContentType = contentType });
                
                return blobClient.Uri.ToString();
            }
        }

        public async Task<bool> DeleteAsync(string fileUrl)
        {
            try
            {
                if (_useLocalStorage)
                {
                    var fileName = Path.GetFileName(fileUrl);
                    var filePath = Path.Combine(_localStoragePath, fileName);
                    
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        return true;
                    }
                    return false;
                }
                else
                {
                    // Azure implementation
                    var uri = new Uri(fileUrl);
                    var connectionString = _configuration["Azure:Storage:ConnectionString"];
                    var containerName = _configuration["Azure:Storage:ContainerName"];
                    
                    var blobServiceClient = new BlobServiceClient(connectionString);
                    var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                    
                    var blobName = uri.LocalPath.Substring(uri.LocalPath.IndexOf(containerName) + containerName.Length + 1);
                    var blobClient = containerClient.GetBlobClient(blobName);
                    
                    var response = await blobClient.DeleteIfExistsAsync();
                    return response.Value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {FileUrl}", fileUrl);
                return false;
            }
        }

        public async Task<Stream> DownloadAsync(string fileUrl)
        {
            if (_useLocalStorage)
            {
                var fileName = Path.GetFileName(fileUrl);
                var filePath = Path.Combine(_localStoragePath, fileName);
                
                if (File.Exists(filePath))
                {
                    return new FileStream(filePath, FileMode.Open, FileAccess.Read);
                }
                
                throw new FileNotFoundException($"File not found: {filePath}");
            }
            else
            {
                var uri = new Uri(fileUrl);
                var connectionString = _configuration["Azure:Storage:ConnectionString"];
                var containerName = _configuration["Azure:Storage:ContainerName"];
                
                var blobServiceClient = new BlobServiceClient(connectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                
                var blobName = uri.LocalPath.Substring(uri.LocalPath.IndexOf(containerName) + containerName.Length + 1);
                var blobClient = containerClient.GetBlobClient(blobName);
                
                var response = await blobClient.DownloadAsync();
                return response.Value.Content;
            }
        }
    }
}
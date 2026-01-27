// src/Monolith.WebAPI/Controllers/TrackingController.cs

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Journeys;
using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.Data.Members;
using Monolith.WebAPI.Services.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Monolith.WebAPI.Controllers;

[ApiController]
[Route("tracking")]
public class TrackingController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<TrackingController> _logger;
    private readonly IConfiguration _configuration;
    private readonly ICloudinaryService _cloudinaryService;

    public TrackingController(
        AppDbContext context, 
        ILogger<TrackingController> logger, 
        IConfiguration configuration,
        ICloudinaryService cloudinaryService)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _cloudinaryService = cloudinaryService;
    }

    [HttpGet("delivery/{journeyId}/{stopId}")]
    public async Task<IActionResult> GetDeliveryDetails(int journeyId, int stopId, [FromQuery] string? token)
    {
        try
        {
            // Token doğrulama
            var expectedToken = GenerateToken(journeyId, stopId);
            if (string.IsNullOrEmpty(token) || token != expectedToken)
            {
                _logger.LogWarning($"Invalid token attempt for journey {journeyId}, stop {stopId}");
                return Content(GenerateErrorPage("Geçersiz veya süresi dolmuş link"), "text/html; charset=utf-8");
            }

            // Multi-tenant güvenlik - workspace kontrolü ekle
            var journey = await _context.Journeys
                .Include(j => j.Workspace)
                .FirstOrDefaultAsync(j => j.Id == journeyId);
                
            if (journey == null)
            {
                _logger.LogWarning($"Journey not found: {journeyId}");
                return Content(GenerateErrorPage("Teslimat bilgisi bulunamadı"), "text/html; charset=utf-8");
            }

            // En son journey status bilgisini al
            var journeyStatus = await _context.JourneyStatuses
                .Include(js => js.Journey)
                    .ThenInclude(j => j.Driver)
                .Include(js => js.Stop)
                    .ThenInclude(s => s.RouteStop)
                        .ThenInclude(rs => rs.Customer)
                .Where(js => js.JourneyId == journeyId && js.StopId == stopId)
                .OrderByDescending(js => js.CreatedAt)
                .FirstOrDefaultAsync();

            if (journeyStatus == null)
            {
                _logger.LogWarning($"Journey status not found for journey {journeyId}, stop {stopId}");
                return Content(GenerateErrorPage("Teslimat bilgisi bulunamadı"), "text/html; charset=utf-8");
            }

            // Workspace bilgisini de log'la
            _logger.LogInformation($"Tracking page accessed for Journey {journeyId}, Stop {stopId}, Workspace {journey.WorkspaceId}");

            var stop = journeyStatus.Stop;
            var customer = stop?.RouteStop?.Customer;
            var driver = journeyStatus.Journey?.Driver;

            // HTML sayfasını oluştur
            var html = await GenerateDeliveryPageAsync(journeyStatus, stop, customer, driver);
            return Content(html, "text/html; charset=utf-8");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting delivery details for journey {journeyId}, stop {stopId}");
            return Content(GenerateErrorPage("Bir hata oluştu"), "text/html; charset=utf-8");
        }
    }

    private async Task<string> GenerateDeliveryPageAsync(JourneyStatus journeyStatus, JourneyStop? stop, Customer? customer, Driver? driver)
    {
        var statusText = journeyStatus.Status switch
        {
            JourneyStatusType.Completed => "✅ Teslim Edildi",
            JourneyStatusType.Cancelled => "❌ Teslim Edilemedi",
            JourneyStatusType.Arrived => "🚚 Teslimatta",
            _ => "📦 İşleniyor"
        };

        var statusClass = journeyStatus.Status switch
        {
            JourneyStatusType.Completed => "status-completed",
            JourneyStatusType.Cancelled => "status-failed",
            JourneyStatusType.Arrived => "status-progress",
            _ => "status-pending"
        };

        // ✅ ReceiverName'i önce entity'den, yoksa AdditionalValues'dan al
        var receiverName = journeyStatus.ReceiverName; // Entity'de ReceiverName property'si varsa
        
        if (string.IsNullOrEmpty(receiverName))
        {
            receiverName = journeyStatus.AdditionalValues?.ContainsKey("ReceiverName") == true 
                ? journeyStatus.AdditionalValues["ReceiverName"] 
                : null;
        }

        return $@"
<!DOCTYPE html>
<html lang='tr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Teslimat Detayları - YolPilot</title>
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        body {{ 
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; 
            line-height: 1.6; 
            color: #333; 
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            padding: 20px;
        }}
        .container {{ 
            max-width: 800px; 
            margin: 0 auto; 
            background: white;
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 20px 40px rgba(0,0,0,0.1);
            animation: fadeIn 0.5s ease-in;
        }}
        @keyframes fadeIn {{
            from {{ opacity: 0; transform: translateY(20px); }}
            to {{ opacity: 1; transform: translateY(0); }}
        }}
        .header {{ 
            background: white;
            padding: 30px;
            text-align: center;
            border-bottom: 2px solid #f0f0f0;
        }}
        .logo {{ 
            font-size: 32px; 
            font-weight: bold; 
            color: #667eea;
            margin-bottom: 10px;
        }}
        .status-badge {{
            display: inline-block;
            padding: 8px 20px;
            border-radius: 20px;
            font-weight: 500;
            margin-top: 10px;
            transition: all 0.3s ease;
        }}
        .status-completed {{ 
            background: linear-gradient(135deg, #d4edda, #c3e6cb); 
            color: #155724; 
            box-shadow: 0 2px 10px rgba(40, 167, 69, 0.2);
        }}
        .status-failed {{ 
            background: linear-gradient(135deg, #f8d7da, #f5c6cb); 
            color: #721c24; 
            box-shadow: 0 2px 10px rgba(220, 53, 69, 0.2);
        }}
        .status-progress {{ 
            background: linear-gradient(135deg, #cce5ff, #b8daff); 
            color: #004085; 
            box-shadow: 0 2px 10px rgba(0, 123, 255, 0.2);
        }}
        .status-pending {{ 
            background: linear-gradient(135deg, #ffeaa7, #fdcb6e); 
            color: #856404; 
            box-shadow: 0 2px 10px rgba(255, 193, 7, 0.2);
        }}
        .content {{ padding: 30px; }}
        .info-section {{ 
            margin-bottom: 30px;
            padding: 20px;
            background: #f8f9fa;
            border-radius: 8px;
            transition: all 0.3s ease;
        }}
        .info-section:hover {{
            transform: translateY(-2px);
            box-shadow: 0 5px 15px rgba(0,0,0,0.1);
        }}
        .info-section h2 {{ 
            color: #667eea; 
            margin-bottom: 15px;
            font-size: 20px;
            display: flex;
            align-items: center;
            gap: 10px;
        }}
        .info-row {{ 
            display: flex; 
            justify-content: space-between; 
            padding: 12px 0;
            border-bottom: 1px solid #e0e0e0;
            transition: background 0.2s ease;
        }}
        .info-row:hover {{
            background: rgba(102, 126, 234, 0.05);
            padding-left: 10px;
            padding-right: 10px;
            margin-left: -10px;
            margin-right: -10px;
        }}
        .info-row:last-child {{ border-bottom: none; }}
        .info-row.highlight {{
            background: linear-gradient(135deg, #e6f3ff, #dbeafe);
            padding: 15px;
            border-radius: 8px;
            margin: 10px 0;
            border: 2px solid #3b82f6;
            border-left-width: 4px;
        }}
        .info-label {{ 
            font-weight: 600; 
            color: #666; 
            display: flex;
            align-items: center;
            gap: 5px;
        }}
        .info-value {{ 
            color: #333; 
            text-align: right;
            max-width: 60%;
        }}
        .receiver-value {{
            color: #2563eb !important;
            font-size: 16px;
            font-weight: 600;
        }}
        .proof-section {{ 
            margin-top: 30px;
            padding: 20px;
            background: #fff;
            border: 2px solid #667eea;
            border-radius: 8px;
            animation: slideUp 0.5s ease-in;
        }}
        @keyframes slideUp {{
            from {{ opacity: 0; transform: translateY(30px); }}
            to {{ opacity: 1; transform: translateY(0); }}
        }}
        .proof-section h2 {{
            color: #667eea;
            margin-bottom: 20px;
            display: flex;
            align-items: center;
            gap: 10px;
        }}
        .proof-grid {{ 
            display: grid; 
            grid-template-columns: repeat(auto-fit, minmax(300px, 1fr)); 
            gap: 20px;
            margin-top: 20px;
        }}
        .proof-item {{
            text-align: center;
            padding: 20px;
            background: #f8f9fa;
            border-radius: 8px;
            transition: all 0.3s ease;
        }}
        .proof-item:hover {{
            transform: scale(1.02);
            box-shadow: 0 10px 20px rgba(0,0,0,0.1);
        }}
        .proof-item h3 {{
            color: #667eea;
            margin-bottom: 15px;
            font-size: 18px;
        }}
        .proof-image {{
            max-width: 100%;
            height: auto;
            max-height: 300px;
            border-radius: 8px;
            border: 2px solid #e0e0e0;
            background: white;
            padding: 10px;
            cursor: zoom-in;
            transition: all 0.3s ease;
            object-fit: contain;
        }}
        .proof-image:hover {{
            transform: scale(1.05);
            box-shadow: 0 10px 30px rgba(0,0,0,0.2);
        }}
        .photo-caption {{
            text-align: center;
            font-size: 14px;
            color: #666;
            margin-top: 10px;
            font-style: italic;
        }}
        .notes-section {{
            margin-top: 20px;
            padding: 20px;
            background: linear-gradient(135deg, #fff3cd, #ffeaa7);
            border-radius: 8px;
            border-left: 4px solid #ffc107;
        }}
        .notes-section h3 {{
            color: #856404;
            margin-bottom: 10px;
            display: flex;
            align-items: center;
            gap: 10px;
        }}
        .notes-section p {{
            color: #664d03;
            line-height: 1.6;
        }}
        .footer {{
            text-align: center;
            padding: 30px;
            background: #f8f9fa;
            color: #666;
            font-size: 14px;
            border-top: 2px solid #f0f0f0;
        }}
        .footer a {{
            color: #667eea;
            text-decoration: none;
            font-weight: 500;
        }}
        .footer a:hover {{
            text-decoration: underline;
        }}
        .no-proof {{
            text-align: center;
            padding: 60px 20px;
            color: #999;
        }}
        .no-proof svg {{
            width: 80px;
            height: 80px;
            margin-bottom: 20px;
            opacity: 0.5;
        }}
        .icon {{
            width: 20px;
            height: 20px;
            display: inline-block;
            vertical-align: middle;
        }}
        @media (max-width: 600px) {{
            .proof-grid {{ grid-template-columns: 1fr; }}
            .content {{ padding: 20px; }}
            .info-row {{ 
                flex-direction: column; 
                gap: 5px;
            }}
            .info-value {{ 
                text-align: left;
                max-width: 100%;
            }}
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='logo'>🚚 YolPilot</div>
            <h1>Teslimat Detayları</h1>
            <div class='status-badge {statusClass}'>
                {statusText}
            </div>
        </div>
        
        <div class='content'>
            <div class='info-section'>
                <h2>📦 Teslimat Bilgileri</h2>
                <div class='info-row'>
                    <span class='info-label'>👤 Müşteri:</span>
                    <span class='info-value'><strong>{customer?.Name ?? "Bilgi yok"}</strong></span>
                </div>
                <div class='info-row'>
                    <span class='info-label'>📍 Adres:</span>
                    <span class='info-value'>{stop?.RouteStop?.Address ?? "Bilgi yok"}</span>
                </div>
                
                {(journeyStatus.Status == JourneyStatusType.Completed && !string.IsNullOrEmpty(receiverName) ? $@"
                <div class='info-row highlight'>
                    <span class='info-label'>✅ Teslim Alan Kişi:</span>
                    <span class='info-value receiver-value'>{receiverName}</span>
                </div>" : "")}
                
                <div class='info-row'>
                    <span class='info-label'>🚗 Teslimat Görevlisi:</span>
                    <span class='info-value'>{driver?.Name ?? "Bilgi yok"}</span>
                </div>
                <div class='info-row'>
                    <span class='info-label'>📅 Tarih:</span>
                    <span class='info-value'>{journeyStatus.CreatedAt.AddHours(3):dd MMMM yyyy, HH:mm}</span>
                </div>
                {(journeyStatus.Status == JourneyStatusType.Cancelled && !string.IsNullOrEmpty(journeyStatus.FailureReason) ? $@"
                <div class='info-row'>
                    <span class='info-label'>⚠️ Teslim Edilememe Nedeni:</span>
                    <span class='info-value' style='color: #dc3545; font-weight: 500;'>{journeyStatus.FailureReason}</span>
                </div>" : "")}
            </div>

            {(!string.IsNullOrEmpty(journeyStatus.Notes) ? $@"
            <div class='notes-section'>
                <h3>📝 Teslimat Notları</h3>
                <p>{journeyStatus.Notes}</p>
            </div>" : "")}

            {await GenerateProofSectionAsync(journeyStatus)}
        </div>
        
        <div class='footer'>
            <p><strong>Bu sayfa teslimat detaylarını göstermektedir.</strong></p>
            <p style='margin-top: 15px;'>
                📧 <a href='mailto:destek@yolpilot.com'>destek@yolpilot.com</a> | 
                📞 <a href='tel:+908501234567'>0850 123 45 67</a>
            </p>
            <p style='margin-top: 15px; color: #999;'>
                &copy; {DateTime.Now.Year} YolPilot. Tüm hakları saklıdır.
            </p>
        </div>
    </div>

    <script>
        // Resimlere tıklandığında büyük göster
        document.querySelectorAll('.proof-image').forEach(img => {{
            img.addEventListener('click', function() {{
                window.open(this.src, '_blank');
            }});
        }});
    </script>
</body>
</html>";
    }

    private async Task<string> GenerateProofSectionAsync(JourneyStatus journeyStatus)
    {
        var hasProof = false;
        var section = @"
            <div class='proof-section'>
                <h2>📸 Teslimat Kanıtları</h2>
                <div class='proof-grid'>";

        // İmza
        if (!string.IsNullOrEmpty(journeyStatus.SignatureUrl))
        {
            hasProof = true;
            var signatureUrl = GetOptimizedUrl(journeyStatus.SignatureUrl, "signature");
            section += $@"
                    <div class='proof-item'>
                        <h3>✍️ İmza</h3>
                        <img src='{signatureUrl}' alt='Teslimat İmzası' class='proof-image' title='Büyütmek için tıklayın' />
                    </div>";
        }

        // Tek fotoğraf (backward compatibility)
        if (!string.IsNullOrEmpty(journeyStatus.PhotoUrl))
        {
            hasProof = true;
            var photoUrl = GetOptimizedUrl(journeyStatus.PhotoUrl, "photo");
            section += $@"
                    <div class='proof-item'>
                        <h3>📷 Teslimat Fotoğrafı</h3>
                        <img src='{photoUrl}' alt='Teslimat Fotoğrafı' class='proof-image' title='Büyütmek için tıklayın' />
                    </div>";
        }

        // ÇOKLU FOTOĞRAFLAR
        try
        {
            var photos = await _context.JourneyStopPhotos
                .Where(p => p.JourneyId == journeyStatus.JourneyId && p.StopId == journeyStatus.StopId)
                .OrderBy(p => p.DisplayOrder)
                .ToListAsync();

            if (photos.Any())
            {
                hasProof = true;
                var photoIndex = 1;
                foreach (var photo in photos)
                {
                    var photoUrl = GetOptimizedUrl(photo.PhotoUrl, "photo");
                    section += $@"
                    <div class='proof-item'>
                        <h3>📷 Fotoğraf {photoIndex}</h3>
                        <img src='{photoUrl}' alt='Teslimat Fotoğrafı {photoIndex}' class='proof-image' title='Büyütmek için tıklayın' />
                        {(!string.IsNullOrEmpty(photo.Caption) ? $"<p class='photo-caption'>{photo.Caption}</p>" : "")}
                    </div>";
                    photoIndex++;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load journey stop photos");
        }

        section += @"
                </div>
            </div>";

        if (hasProof)
        {
            return section;
        }
        else if (journeyStatus.Status == JourneyStatusType.Completed)
        {
            return @"
            <div class='proof-section'>
                <h2>📸 Teslimat Kanıtları</h2>
                <div class='no-proof'>
                    <svg xmlns='http://www.w3.org/2000/svg' fill='none' viewBox='0 0 24 24' stroke='currentColor'>
                        <path stroke-linecap='round' stroke-linejoin='round' stroke-width='1' d='M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z' />
                    </svg>
                    <p>Bu teslimat için görsel kanıt bulunmamaktadır.</p>
                    <p style='font-size: 12px; margin-top: 10px;'>Teslimat başarıyla tamamlanmıştır.</p>
                </div>
            </div>";
        }

        return "";
    }

    private string GenerateErrorPage(string errorMessage)
    {
        return $@"
<!DOCTYPE html>
<html lang='tr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Hata - YolPilot</title>
    <style>
        body {{ 
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; 
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            padding: 20px;
        }}
        .error-container {{
            background: white;
            padding: 40px;
            border-radius: 12px;
            box-shadow: 0 20px 40px rgba(0,0,0,0.1);
            text-align: center;
            max-width: 500px;
        }}
        .error-icon {{
            font-size: 64px;
            margin-bottom: 20px;
        }}
        h1 {{
            color: #333;
            margin-bottom: 10px;
        }}
        p {{
            color: #666;
            margin-bottom: 30px;
        }}
        .back-link {{
            display: inline-block;
            padding: 12px 30px;
            background: #667eea;
            color: white;
            text-decoration: none;
            border-radius: 6px;
            transition: all 0.3s ease;
        }}
        .back-link:hover {{
            background: #5a67d8;
            transform: translateY(-2px);
        }}
    </style>
</head>
<body>
    <div class='error-container'>
        <div class='error-icon'>⚠️</div>
        <h1>Hata</h1>
        <p>{errorMessage}</p>
        <a href='https://yolpilot.com' class='back-link'>Ana Sayfaya Dön</a>
    </div>
</body>
</html>";
    }

    private string GenerateToken(int journeyId, int stopId)
    {
        // Journey'nin workspace ID'sini al
        var journey = _context.Journeys.FirstOrDefault(j => j.Id == journeyId);
        var workspaceId = journey?.WorkspaceId ?? 0;
        
        var secret = _configuration["Tracking:Secret"] ?? "YolPilot2024Secret!";
        var input = $"{journeyId}-{stopId}-{workspaceId}-{secret}";
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hash).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private string GetOptimizedUrl(string url, string type)
    {
        if (string.IsNullOrEmpty(url)) return "";
        
        // Cloudinary URL ise optimize et
        if (url.Contains("cloudinary.com"))
        {
            try
            {
                var publicId = ExtractPublicIdFromUrl(url);
                if (!string.IsNullOrEmpty(publicId))
                {
                    if (type == "signature")
                    {
                        // İmza için aspect ratio korunarak fit edilsin
                        return _cloudinaryService.GetOptimizedUrl(publicId, 
                            new CloudinaryTransformation 
                            { 
                                Width = 500, 
                                Height = 250, 
                                Crop = "fit",
                                Quality = "auto", 
                                Format = "auto",
                                Gravity = "center"
                            });
                    }
                    else
                    {
                        return _cloudinaryService.GetOptimizedUrl(publicId, 
                            new CloudinaryTransformation 
                            { 
                                Width = 600, 
                                Height = 400, 
                                Crop = "limit",
                                Quality = "auto", 
                                Format = "auto" 
                            });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to optimize Cloudinary URL: {Url}", url);
            }
        }
        
        return GetFullUrl(url);
    }

    private string ExtractPublicIdFromUrl(string url)
    {
        try
        {
            // Cloudinary URL format: https://res.cloudinary.com/{cloud-name}/image/upload/{version}/{public-id}.{format}
            var uri = new Uri(url);
            var segments = uri.AbsolutePath.Split('/');
            
            // Find the index of "upload" and get everything after it
            var uploadIndex = Array.IndexOf(segments, "upload");
            if (uploadIndex >= 0 && uploadIndex < segments.Length - 1)
            {
                // Skip version if it starts with 'v' followed by numbers
                var startIndex = uploadIndex + 1;
                if (segments[startIndex].StartsWith("v") && segments[startIndex].Length > 1 && segments[startIndex].Skip(1).All(char.IsDigit))
                {
                    startIndex++;
                }
                
                // Join remaining segments and remove file extension
                var publicIdWithExtension = string.Join("/", segments.Skip(startIndex));
                var lastDotIndex = publicIdWithExtension.LastIndexOf('.');
                if (lastDotIndex > 0)
                {
                    return publicIdWithExtension.Substring(0, lastDotIndex);
                }
                return publicIdWithExtension;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract public ID from Cloudinary URL: {Url}", url);
        }
        
        return string.Empty;
    }

    private string GetFullUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return "";
        if (url.StartsWith("http://") || url.StartsWith("https://")) return url;
        
        // Azure Blob Storage URL'si ise direkt dön
        if (url.Contains("blob.core.windows.net")) return url;
        
        // Cloudinary URL'si ise direkt dön
        if (url.Contains("cloudinary.com")) return url;
        
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return $"{baseUrl}{url}";
    }
}
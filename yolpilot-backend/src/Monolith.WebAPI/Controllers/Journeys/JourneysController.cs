// src/Monolith.WebAPI/Controllers/Journeys/JourneysController.cs
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Monolith.WebAPI.Applications.Commands.Journeys;
using Monolith.WebAPI.Applications.Queries.Journeys;
using Monolith.WebAPI.Data.Journeys;
using Monolith.WebAPI.Extensions;
using Monolith.WebAPI.Responses.Journeys;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Monolith.WebAPI.Controllers.Journeys;

[Route("api/workspace/journeys")]
[ApiController]
[Authorize(AuthenticationSchemes = "Bearer")]
[SwaggerControllerOrder(6)]
public class JourneysController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;
    
    public JourneysController(ISender sender, IConfiguration configuration, AppDbContext context)
    {
        _sender = sender;
        _configuration = configuration;
        _context = context;
    }
    
    #region Journey Endpoints

    /// <summary>
    /// Get journeys with optional filters
    /// Driver sees only their journeys, others see all
    /// </summary>
    [HttpGet]
    [SwaggerOperation(Summary = "Get the journey by date range.")]
    public async Task<IEnumerable<JourneyResponse>> GetJourneys(
        [FromQuery] DateTime? from, 
        [FromQuery] DateTime? to,
        [FromQuery] string? status,
        [FromQuery] int? driverId,
        [FromQuery] int? vehicleId)
    {
        var query = new GetJourneysQuery
        {
            AuthenticatedUserId = User.GetId(),
            From = from,
            To = to,
            Status = status,
            DriverId = driverId,
            VehicleId = vehicleId
        };
        return await _sender.Send(query);
    }

    /// <summary>
    /// Get journey summaries - lightweight version for dashboard
    /// </summary>
    [HttpGet("summary")]
    [SwaggerOperation(Summary = "Get journey summaries for dashboard and lists")]
    public async Task<IEnumerable<Applications.Queries.Journeys.JourneySummaryResponse>> GetJourneysSummary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? status,
        [FromQuery] int? driverId)
    {
        var query = new GetJourneysSummaryQuery
        {
            AuthenticatedUserId = User.GetId(),
            FromDate = from,
            ToDate = to,
            Status = status,
            DriverId = driverId
        };
        return await _sender.Send(query);
    }

    /// <summary>
    /// Get single journey detail
    /// Driver can only see their own journey
    /// </summary>
    [HttpGet("{journeyId:int}")]
    [SwaggerOperation(Summary = "Get the journey by id.")]
    public async Task<JourneyResponse> GetJourney(int journeyId)
    {
        var query = new GetJourneyQuery
        {
            AuthenticatedUserId = User.GetId(),
            JourneyId = journeyId
        };
        return await _sender.Send(query);
    }

    
    [HttpGet("{id}/static-map")]
    public async Task<IActionResult> GetStaticMapUrl(int id)
    {
        // Mevcut GetJourney endpoint'ini kullan
        var query = new GetJourneyQuery
        {
            AuthenticatedUserId = User.GetId(),
            JourneyId = id
        };
        var journey = await _sender.Send(query);
        
        if (journey == null)
        {
            return NotFound();
        }
        
        // Google Static Maps URL oluştur
        var baseUrl = "https://maps.googleapis.com/maps/api/staticmap";
        var size = "600x400";
        var mapType = "roadmap";
        
        // Polyline'ı encode edilmiş olarak kullan
        var encodedPath = journey.Polyline; // Zaten encoded geliyor
        
        // Marker'lar için stop'ları ekle
        var markers = new List<string>();
        
        // Başlangıç (Depo)
        if (journey.Route?.Depot != null)
        {
            markers.Add($"color:green|label:D|{journey.Route.Depot.Latitude},{journey.Route.Depot.Longitude}");
        }
        
        // Excluded olmayan stops
        var activeStops = journey.Stops
            .Where(s => s.Order > 0)
            .OrderBy(s => s.Order);

        // Foreach DIŞINDA hesapla
        var journeyInProgress = journey.Status.ToString().ToLower() == "inprogress";
        var firstPendingOrder = activeStops  // ⚠️ journey.Stops DEĞİL, activeStops kullan
            .Where(s => s.Status?.ToLower() == "pending")
            .FirstOrDefault()?.Order ?? -1;

        foreach (var stop in activeStops)
        {
            var isNextStop = journeyInProgress && 
                            stop.Status?.ToLower() == "pending" && 
                            stop.Order == firstPendingOrder;

            var color = stop.Status?.ToLower() switch  // ToLower ekle
            {
                "completed" => "blue",
                "failed" => "red",
                "in_progress" or "inprogress" => "yellow",
                "pending" when isNextStop => "orange",  // Sıradaki durak
                "pending" => "gray",  // Diğer bekleyen duraklar
                _ => "gray"
            };
            
            markers.Add($"color:{color}|label:{stop.Order}|{stop.EndLatitude},{stop.EndLongitude}");
        }
        
        // URL oluştur
        var url = $"{baseUrl}?size={size}&maptype={mapType}";
        
        if (!string.IsNullOrEmpty(encodedPath))
        {
            url += $"&path=color:0x0000ff|weight:3|enc:{encodedPath}";
        }
        
        foreach (var marker in markers)
        {
            url += $"&markers={marker}";
        }
        
        url += $"&key={_configuration["GoogleMaps:ApiKey"]}";
        
        return Ok(new { 
            staticMapUrl = url,
            hasPolyline = !string.IsNullOrEmpty(encodedPath),
            stopsCount = activeStops.Count()
        });
    }
    
    /// <summary>
    /// Get journey status history
    /// </summary>
    [HttpGet("{journeyId:int}/statuses")]
    [SwaggerOperation(Summary = "Get the journey statuses.")]
    public async Task<IEnumerable<JourneyStatusResponse>> GetStatuses(int journeyId)
    {
        var query = new GetJourneyStatusesQuery
        {
            AuthenticatedUserId = User.GetId(),
            JourneyId = journeyId
        };
        return await _sender.Send(query);
    }

    /// <summary>
    /// ✅ YENİ - Get stop details including signature and photos
    /// </summary>
    [HttpGet("{journeyId:int}/stops/{stopId:int}/details")]
    [SwaggerOperation(Summary = "Get stop details including signature, photos and receiver info")]
    [Authorize]
    public async Task<IActionResult> GetStopDetails(
        [FromRoute] int journeyId, 
        [FromRoute] int stopId)
    {
        var status = await _context.JourneyStatuses
            .Where(s => s.JourneyId == journeyId && s.StopId == stopId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new
            {
                s.SignatureUrl,
                s.PhotoUrl,
                s.ReceiverName,  // ✅ YENİ ALAN
                s.Notes,
                s.FailureReason,
                s.Status,
                s.CreatedAt
            })
            .FirstOrDefaultAsync();
        
        return Ok(status);
    }

    /// <summary>
    /// Assign a route to driver and create journey
    /// Only Dispatcher and above can assign
    /// </summary>
    [HttpPost("assignment")]
    [SwaggerOperation(Summary = "Assign a route to the driver, and create a new journey.")]
    [Authorize(Roles = "Dispatcher,Admin,SuperAdmin")]
    public async Task<JourneyResponse> AssignRoute(AssignRouteCommand command)
    {
        command.AuthenticatedUserId = User.GetId();
        return await _sender.Send(command);
    }

    /// <summary>
    /// Start journey - Driver or Dispatcher can start
    /// </summary>
    [HttpPost("{journeyId:int}/start")]
    [SwaggerOperation(Summary = "Start the journey. Use this endpoint when the driver starts the journey.")]
    public async Task<JourneyResponse> StartJourney(int journeyId)
    {
        var command = new StartJourneyCommand
        {
            JourneyId = journeyId,
            AuthenticatedUserId = User.GetId()
        };
        return await _sender.Send(command);
    }

    /// <summary>
    /// Finish journey - Driver or Dispatcher can finish
    /// </summary>
    [HttpPost("{journeyId:int}/finish")]
    [SwaggerOperation(Summary = "Finish the journey. Use this endpoint when the driver finishes the journey.")]
    public async Task<JourneyResponse> FinishJourney(int journeyId)
    {
        var command = new FinishJourneyCommand
        {
            JourneyId = journeyId,
            AuthenticatedUserId = User.GetId()
        };
        return await _sender.Send(command);
    }

    /// <summary>
    /// Cancel journey - Dispatcher and above can cancel
    /// </summary>
    [HttpPost("{journeyId:int}/cancel")]
    [SwaggerOperation(Summary = "Cancel the journey")]
    [Authorize(Roles = "Dispatcher,Admin,SuperAdmin")]
    public async Task<JourneyResponse> CancelJourney(int journeyId)
    {
        var command = new CancelJourneyCommand
        {
            JourneyId = journeyId,
            AuthenticatedUserId = User.GetId()
        };
        return await _sender.Send(command);
    }

    /// <summary>
    /// ✅ YENİ - Add stop to active journey
    /// Only Dispatcher and above can add stops to active journeys
    /// </summary>
    [HttpPost("{journeyId:int}/stops")]
    [SwaggerOperation(Summary = "Add a new stop to an active journey")]
    [Authorize(Roles = "Dispatcher,Admin,SuperAdmin")]
    public async Task<JourneyResponse> AddStopToActiveJourney(int journeyId, [FromBody] AddStopToActiveJourneyRequest request)
    {
        var command = new AddStopToActiveJourneyCommand
        {
            JourneyId = journeyId,
            AuthenticatedUserId = User.GetId(),
            CustomerId = request.CustomerId,
            Address = request.Address,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            ServiceTimeMinutes = request.ServiceTimeMinutes,
            Notes = request.Notes
        };
        return await _sender.Send(command);
    }

    /// <summary>
    /// ✅ YENİ - Remove a stop from journey (only pending stops can be removed)
    /// </summary>
    [HttpDelete("{journeyId:int}/stops/{stopId:int}")]
    [SwaggerOperation(Summary = "Remove a pending stop from journey")]
    [Authorize(Roles = "Dispatcher,Admin,SuperAdmin")]
    public async Task<JourneyResponse> RemoveStopFromJourney(int journeyId, int stopId)
    {
        var command = new RemoveStopFromJourneyCommand
        {
            JourneyId = journeyId,
            StopId = stopId,
            AuthenticatedUserId = User.GetId()
        };
        return await _sender.Send(command);
    }

    /// <summary>
    /// ✅ YENİ - Reoptimize active journey based on driver's current location
    /// Driver or Dispatcher can trigger reoptimization
    /// </summary>
    [HttpPost("{journeyId:int}/reoptimize")]
    [SwaggerOperation(Summary = "Reoptimize remaining stops based on driver's current location")]
    public async Task<JourneyResponse> ReoptimizeActiveJourney(int journeyId, [FromBody] ReoptimizeActiveJourneyRequest request)
    {
        var command = new ReoptimizeActiveJourneyCommand
        {
            JourneyId = journeyId,
            AuthenticatedUserId = User.GetId(),
            CurrentLatitude = request.CurrentLatitude,
            CurrentLongitude = request.CurrentLongitude
        };
        return await _sender.Send(command);
    }

    /// <summary>
    /// Add journey status update
    /// </summary>
    [HttpPost("{journeyId:int}/status")]
    [SwaggerOperation(Summary = "Add the status of the stop. Use this endpoint when the driver wants to update the status of the stop.")]
    public async Task<JourneyStatusResponse> AddStatus(int journeyId, [FromBody] AddJourneyStatusCommand command)
    {
        command.JourneyId = journeyId;
        command.AuthenticatedUserId = User.GetId();
        return await _sender.Send(command);
    }

    /// <summary>
    /// Optimize route - Dispatcher and above
    /// </summary>
    [HttpPost("{journeyId:int}/optimize")]
    [SwaggerOperation(Summary = "Optimize the route for the journey.")]
    [Authorize(Roles = "Dispatcher,Admin,SuperAdmin")]
    public async Task<JourneyResponse> OptimizeRoute(int journeyId)
    {
        var command = new OptimizeJourneyCommand
        {
            JourneyId = journeyId,
            AuthenticatedUserId = User.GetId()
        };
        return await _sender.Send(command);
    }

    /// <summary>
    /// Optimize journey for time deviation
    /// </summary>
    [HttpPost("{id}/optimize-for-deviation")]
    public async Task<IActionResult> OptimizeForDeviation(int id, [FromBody] OptimizeForDeviationRequest request)
    {
        var command = new OptimizeJourneyForDeviationCommand
        {
            JourneyId = id,
            ActualStartTime = request?.ActualStartTime, // ÖNEMLİ: body'den al
            AuthenticatedUserId = User.GetId()
        };

        var result = await _sender.Send(command);
        return Ok(result);
    }

    #endregion

    #region Stop Management Endpoints - V38 YENİ

    /// <summary>
    /// ✅ V38 - Check-in to a stop
    /// Driver checks in when arriving at stop
    /// </summary>
    [HttpPost("{journeyId:int}/stops/{stopId:int}/checkin")]
    [SwaggerOperation(Summary = "Check in to a stop - marks arrival")]
    public async Task<Responses.Journeys.CheckInResponse> CheckInStop(int journeyId, int stopId)
    {
        var command = new CheckInJourneyStopCommand
        {
            JourneyId = journeyId,
            StopId = stopId,
            AuthenticatedUserId = User.GetId()
        };
        return await _sender.Send(command);
    }

    /// <summary>
    /// ✅ YENİ - Submit delay reason for a stop
    /// Driver submits reason for significant delay (15+ minutes)
    /// </summary>
    [HttpPost("{journeyId:int}/stops/{stopId:int}/delay-reason")]
    [SwaggerOperation(Summary = "Submit delay reason for stop")]
    public async Task<bool> SubmitDelayReason(
        int journeyId,
        int stopId,
        [FromBody] SubmitDelayReasonRequest request)
    {
        var command = new SubmitDelayReasonCommand
        {
            JourneyId = journeyId,
            StopId = stopId,
            DelayReasonCategory = request.DelayReasonCategory,
            DelayReason = request.DelayReason,
            AuthenticatedUserId = User.GetId()
        };
        return await _sender.Send(command);
    }

    /// <summary>
    /// ✅ V38 - Complete a stop with signature and photo (FormData) - ÇOKLU FOTOĞRAF DESTEĞİ
    /// Driver completes delivery with proof
    /// </summary>
    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpPost("{journeyId:int}/stops/{stopId:int}/complete")]
    [SwaggerOperation(Summary = "Complete a stop with optional signature and multiple photos")]
    [Consumes("multipart/form-data")]
    public async Task<bool> CompleteStop(
        int journeyId,
        int stopId,
        [FromForm] string? notes,
        [FromForm] string? receiverName,  // ✅ YENİ - Teslim alan kişi
        [FromForm] IFormFile? signature,
        [FromForm] IFormFile? photo,     // Backward compatibility için tek fotoğraf
        [FromForm] List<IFormFile>? photos, // YENİ: Çoklu fotoğraf desteği
        [FromForm] decimal? endKilometer, // ✅ YENİ - Depo dönüşü için kilometre
        [FromForm] string? fuelLevel, // ✅ YENİ - Depo dönüşü için yakıt seviyesi
        [FromForm] string? vehicleCondition) // ✅ YENİ - Depo dönüşü için araç durumu
    {
        // Tüm fotoğrafları birleştir
        var allPhotos = new List<IFormFile>();

        // Eski API uyumluluğu için tek photo parametresi
        if (photo != null)
        {
            allPhotos.Add(photo);
        }

        // Yeni çoklu fotoğraf parametresi
        if (photos != null && photos.Any())
        {
            allPhotos.AddRange(photos);
        }

        var command = new CompleteJourneyStopCommand
        {
            JourneyId = journeyId,
            StopId = stopId,
            Notes = notes,
            ReceiverName = receiverName,  // ✅ YENİ
            SignatureFile = signature,
            PhotoFile = allPhotos.FirstOrDefault(), // İlk fotoğraf (backward compatibility)
            PhotoFiles = allPhotos.Count > 1 ? allPhotos.Skip(1).ToList() : null, // İlk fotoğraf hariç diğerleri
            EndKilometer = endKilometer, // ✅ YENİ - Depo dönüşü için
            FuelLevel = fuelLevel, // ✅ YENİ - Depo dönüşü için
            VehicleCondition = vehicleCondition, // ✅ YENİ - Depo dönüşü için
            AuthenticatedUserId = User.GetId()
        };
        return await _sender.Send(command);
    }

    /// <summary>
    /// ✅ V38 - Mark a stop as failed
    /// Driver marks delivery as failed with reason
    /// </summary>
    [HttpPost("{journeyId:int}/stops/{stopId:int}/fail")]
    [SwaggerOperation(Summary = "Mark a stop as failed with reason")]
    public async Task<bool> FailStop(
        int journeyId, 
        int stopId,
        [FromBody] FailStopRequest request)
    {
        var command = new FailJourneyStopCommand
        {
            JourneyId = journeyId,
            StopId = stopId,
            FailureReason = request.FailureReason,
            Notes = request.Notes,
            AuthenticatedUserId = User.GetId()
        };
        return await _sender.Send(command);
    }

    /// <summary>
    /// ✅ V38 - Update a stop with detailed information (FormData) - ÇOKLU FOTOĞRAF DESTEĞİ
    /// Generic endpoint for any stop update
    /// </summary>
    [HttpPut("{journeyId:int}/stops/{stopId:int}")]
    [SwaggerOperation(Summary = "Update a stop with all details including files")]
    [Consumes("multipart/form-data")]
    public async Task<bool> UpdateStop(
        int journeyId,
        int stopId,
        [FromForm] UpdateStopRequest request,
        IFormFile? signature,
        IFormFile? photo,     // Backward compatibility için tek fotoğraf
        List<IFormFile>? photos) // YENİ: Çoklu fotoğraf desteği
    {
        // Tüm fotoğrafları birleştir
        var allPhotos = new List<IFormFile>();
        
        // Eski API uyumluluğu için tek photo parametresi
        if (photo != null)
        {
            allPhotos.Add(photo);
        }
        
        // Yeni çoklu fotoğraf parametresi
        if (photos != null && photos.Any())
        {
            allPhotos.AddRange(photos);
        }
        
        var command = new UpdateJourneyStopCommand
        {
            JourneyId = journeyId,
            StopId = stopId,
            Status = request.Status,
            CheckInTime = request.CheckInTime,
            CheckOutTime = request.CheckOutTime,
            Notes = request.Notes,
            ReceiverName = request.ReceiverName,  // ✅ YENİ
            FailureReason = request.FailureReason,
            SignatureFile = signature,
            PhotoFile = allPhotos.FirstOrDefault(), // İlk fotoğraf (backward compatibility)
            PhotoFiles = allPhotos.Count > 1 ? allPhotos.Skip(1).ToList() : null, // İlk fotoğraf hariç diğerleri
            AuthenticatedUserId = User.GetId()
        };
        return await _sender.Send(command);
    }

    #endregion

    #region Photo Management Endpoints - YENİ

    /// <summary>
    /// Get photos for a specific stop - DUPLICATE KONTROLÜ EKLENDİ
    /// </summary>
    [HttpGet("{journeyId:int}/stops/{stopId:int}/photos")]
    [SwaggerOperation(Summary = "Get all photos for a specific stop")]
    public async Task<IActionResult> GetStopPhotos(
        [FromRoute] int journeyId, 
        [FromRoute] int stopId)
    {
        var photos = await _context.JourneyStopPhotos
            .Where(p => p.JourneyId == journeyId && p.StopId == stopId && !p.IsDeleted)
            .OrderBy(p => p.DisplayOrder)
            .Select(p => new
            {
                p.Id,
                p.PhotoUrl,
                p.ThumbnailUrl,
                p.Caption,
                p.DisplayOrder,
                p.CreatedAt
            })
            .ToListAsync();
        
        // İlk fotoğraf JourneyStatus'tan gelebilir
        var journeyStatus = await _context.JourneyStatuses
            .Where(s => s.JourneyId == journeyId && s.StopId == stopId && s.PhotoUrl != null)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();
        
        var allPhotos = new List<object>();
        
        // ✅ DÜZELTME: Ana fotoğrafı sadece JourneyStopPhotos'ta yoksa ekle
        if (journeyStatus?.PhotoUrl != null)
        {
            // PhotoUrl'in JourneyStopPhotos'ta olup olmadığını kontrol et
            var photoExistsInStopPhotos = photos.Any(p => p.PhotoUrl == journeyStatus.PhotoUrl);
            
            if (!photoExistsInStopPhotos)
            {
                allPhotos.Add(new
                {
                    Id = 0,
                    PhotoUrl = journeyStatus.PhotoUrl,
                    ThumbnailUrl = journeyStatus.PhotoUrl,
                    Caption = "Ana fotoğraf",
                    DisplayOrder = 0,
                    CreatedAt = journeyStatus.CreatedAt
                });
            }
        }
        
        allPhotos.AddRange(photos);
        
        return Ok(allPhotos);
    }

    /// <summary>
    /// Get all photos for a journey
    /// </summary>
    [HttpGet("{journeyId:int}/photos")]
    [SwaggerOperation(Summary = "Get all photos for a journey")]
    public async Task<IActionResult> GetAllJourneyPhotos([FromRoute] int journeyId)
    {
        var photos = await _context.JourneyStopPhotos
            .Where(p => p.JourneyId == journeyId && !p.IsDeleted)
            .OrderBy(p => p.StopId)
            .ThenBy(p => p.DisplayOrder)
            .Select(p => new
            {
                p.Id,
                p.StopId,
                p.PhotoUrl,
                p.ThumbnailUrl,
                p.Caption,
                p.DisplayOrder,
                p.CreatedAt,
                StopName = p.Stop.EndAddress
            })
            .ToListAsync();
        
        return Ok(photos);
    }

    /// <summary>
    /// Delete a photo
    /// </summary>
    [HttpDelete("{journeyId:int}/stops/{stopId:int}/photos/{photoId:int}")]
    [SwaggerOperation(Summary = "Delete a specific photo")]
    [Authorize(Roles = "Driver,Dispatcher,Admin,SuperAdmin")]
    public async Task<IActionResult> DeletePhoto(
        [FromRoute] int journeyId,
        [FromRoute] int stopId,
        [FromRoute] int photoId)
    {
        var photo = await _context.JourneyStopPhotos
            .FirstOrDefaultAsync(p => 
                p.Id == photoId && 
                p.JourneyId == journeyId && 
                p.StopId == stopId);
        
        if (photo == null)
        {
            return NotFound("Photo not found");
        }
        
        _context.JourneyStopPhotos.Remove(photo);
        await _context.SaveChangesAsync();
        
        return Ok(new { success = true, message = "Photo deleted successfully" });
    }

    #endregion

    #region Bulk Operations - YENİ

    /// <summary>
    /// Toplu iptal işlemi - Seçili seferleri iptal eder
    /// Only Dispatcher and above can bulk cancel
    /// </summary>
    [HttpPost("bulk/cancel")]
    [SwaggerOperation(Summary = "Cancel multiple journeys at once")]
    [Authorize(Roles = "Dispatcher,Admin,SuperAdmin")]
    public async Task<Applications.Commands.Journeys.BulkOperationResult> BulkCancel([FromBody] BulkCancelJourneysCommand command)
    {
        command.AuthenticatedUserId = User.GetId();
        return await _sender.Send(command);
    }

    /// <summary>
    /// Toplu arşivleme işlemi - Seçili seferleri arşivler
    /// Only Admin and above can bulk archive
    /// </summary>
    [HttpPost("bulk/archive")]
    [SwaggerOperation(Summary = "Archive multiple journeys at once")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<Applications.Commands.Journeys.BulkOperationResult> BulkArchive([FromBody] BulkArchiveJourneysCommand command)
    {
        command.AuthenticatedUserId = User.GetId();
        return await _sender.Send(command);
    }

    /// <summary>
    /// Toplu silme işlemi - Seçili seferleri kalıcı olarak siler
    /// DİKKAT: Bu işlem geri alınamaz! Only Admin and above
    /// </summary>
    [HttpDelete("bulk/delete")]
    [SwaggerOperation(Summary = "Permanently delete multiple journeys at once")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<Applications.Commands.Journeys.BulkOperationResult> BulkDelete([FromBody] BulkDeleteJourneysCommand command)
    {
        command.AuthenticatedUserId = User.GetId();
        return await _sender.Send(command);
    }
    
    /// <summary>
    /// Reset a journey stop (from Failed or InProgress to Pending)
    /// </summary>
    [HttpPost("{journeyId}/stops/{stopId}/reset")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetStop([FromRoute] int journeyId, [FromRoute] int stopId)
    {
      var command = new ResetJourneyStopCommand 
      { 
          JourneyId = journeyId, 
          StopId = stopId,
          AuthenticatedUserId = User.GetId()
      };
    
       var result = await _sender.Send(command);
       return Ok(result);
    }

    #endregion
}

// ✅ V38 - Request DTO'ları
public class FailStopRequest
{
    public string FailureReason { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class UpdateStopRequest
{
    public JourneyStopStatus Status { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string? Notes { get; set; }
    public string? ReceiverName { get; set; }  // ✅ YENİ ALAN
    public string? FailureReason { get; set; }
}

public class OptimizeForDeviationRequest
{
    public string ActualStartTime { get; set; }
}

// ✅ YENİ - Add stop to active journey request
public class AddStopToActiveJourneyRequest
{
    public int CustomerId { get; set; }
    public string Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int? ServiceTimeMinutes { get; set; }
    public string? Notes { get; set; }
}

// ✅ YENİ - Reoptimize active journey request
public class ReoptimizeActiveJourneyRequest
{
    public double CurrentLatitude { get; set; }
    public double CurrentLongitude { get; set; }
}
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Journeys;
using Monolith.WebAPI.Data.Workspace.Enums;
using Monolith.WebAPI.Data.Workspace;
using MediatR;
using Monolith.WebAPI.Hubs;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Services.Members;
using Monolith.WebAPI.Services.Storage;
using Monolith.WebAPI.Services.WhatsApp;
using Monolith.WebAPI.Services.Subscription;
using Monolith.WebAPI.Services.Email;
using Monolith.WebAPI.Services.Templates;
using Monolith.WebAPI.Services.Feedback;
using Microsoft.Extensions.Configuration;

namespace Monolith.WebAPI.Applications.Commands.Journeys;

public class UpdateJourneyStopCommand : BaseAuthenticatedCommand<bool>
{
    [JsonIgnore] public override bool RequiresDriver => true;
    [JsonIgnore] public override bool RequiresWorkspaceAccess => true;
    [JsonIgnore] public int JourneyId { get; set; }
    [JsonIgnore] public int StopId { get; set; }

    public JourneyStopStatus Status { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string? Notes { get; set; }
    public string? FailureReason { get; set; }
    public string? ReceiverName { get; set; } // ✅ YENİ EKLENEN

    [JsonIgnore] public IFormFile? SignatureFile { get; set; }
    [JsonIgnore] public IFormFile? PhotoFile { get; set; }

    // YENİ: Çoklu fotoğraf desteği
    [JsonIgnore] public List<IFormFile>? PhotoFiles { get; set; }

    // ✅ YENİ: Depo dönüşü için özel alanlar
    public decimal? EndKilometer { get; set; }
    public string? FuelLevel { get; set; }
    public string? VehicleCondition { get; set; }
}

public class UpdateJourneyStopCommandValidator : AbstractValidator<UpdateJourneyStopCommand>
{
    public UpdateJourneyStopCommandValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.FailureReason).MaximumLength(200);
        RuleFor(x => x.ReceiverName).MaximumLength(200); // ✅ YENİ EKLENEN
        
        // YENİ: Çoklu fotoğraf validasyonu
        RuleFor(x => x.PhotoFiles)
            .Must(files => files == null || files.Count <= 10)
            .WithMessage("En fazla 10 fotoğraf yükleyebilirsiniz.");
    }
}

public class UpdateJourneyStopCommandHandler : BaseAuthenticatedCommandHandler<UpdateJourneyStopCommand, bool>
{
    private readonly AppDbContext _context;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly IBlobStorageService _blobStorage;
    private readonly IHubContext<JourneyHub> _hubContext;
    private readonly IEmailService _emailService;
    private readonly IWhatsAppService _whatsAppService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ITemplateService _templateService;
    private readonly IFeedbackService _feedbackService;
    private readonly ILogger<UpdateJourneyStopCommandHandler> _logger;
    private readonly IConfiguration _configuration;
    private readonly ISender _sender;

    public UpdateJourneyStopCommandHandler(
        AppDbContext context,
        ICloudinaryService cloudinaryService,
        IBlobStorageService blobStorage,
        IHubContext<JourneyHub> hubContext,
        IUserService userService,
        IEmailService emailService,
        IWhatsAppService whatsAppService,
        ISubscriptionService subscriptionService,
        ITemplateService templateService,
        IFeedbackService feedbackService,
        ILogger<UpdateJourneyStopCommandHandler> logger,
        IConfiguration configuration,
        ISender sender)
        : base(userService)
    {
        _context = context;
        _cloudinaryService = cloudinaryService;
        _blobStorage = blobStorage;
        _hubContext = hubContext;
        _emailService = emailService;
        _whatsAppService = whatsAppService;
        _subscriptionService = subscriptionService;
        _templateService = templateService;
        _feedbackService = feedbackService;
        _logger = logger;
        _configuration = configuration;
        _sender = sender;
    }

    protected override async Task<bool> HandleCommand(UpdateJourneyStopCommand request, CancellationToken cancellationToken)
    {
        var stop = await _context.JourneyStops
            .Include(s => s.Journey)
                .ThenInclude(j => j.Driver)
            .Include(s => s.Journey)
                .ThenInclude(j => j.Vehicle)
            .Include(s => s.RouteStop)
                .ThenInclude(rs => rs.Customer)
            .FirstOrDefaultAsync(s =>
                s.Id == request.StopId &&
                s.JourneyId == request.JourneyId &&
                s.Journey.WorkspaceId == User.WorkspaceId,
                cancellationToken);

        if (stop == null)
        {
            throw new ApiException($"Stop {request.StopId} not found in journey {request.JourneyId}", 404);
        }

        if (stop.Journey.Status != JourneyStatusEnum.InProgress)
        {
            throw new ApiException("Journey is not in progress", 400);
        }

        // ✅ YENİ: Depo dönüşü kontrolü ve araç kilometresi güncelleme
        var maxOrder = await _context.JourneyStops
            .Where(s => s.JourneyId == request.JourneyId)
            .MaxAsync(s => (int?)s.Order, cancellationToken) ?? 0;
        var isLastStop = maxOrder > 0 && stop.Order == maxOrder;
        var isDepotReturn = stop.RouteStop == null || isLastStop;
        if (isDepotReturn && request.Status == JourneyStopStatus.Completed && request.EndKilometer.HasValue)
        {
            if (stop.Journey.Vehicle != null)
            {
                // Araç kilometresini güncelle
                stop.Journey.Vehicle.CurrentKm = (int)request.EndKilometer.Value;
                _logger.LogInformation("Updated vehicle {VehicleId} CurrentKm to {Km} from depot return",
                    stop.Journey.Vehicle.Id, stop.Journey.Vehicle.CurrentKm);

                // ✅ Sefer bitiş kilometresini güncelle
                stop.Journey.EndKm = (int)request.EndKilometer.Value;
                _logger.LogInformation("Updated journey {JourneyId} EndKm to {Km}",
                    stop.Journey.Id, stop.Journey.EndKm);

                // ✅ YENİ - Yakıt ve araç durumu bilgilerini kaydet
                if (!string.IsNullOrEmpty(request.FuelLevel))
                {
                    stop.Journey.EndFuel = request.FuelLevel;
                    _logger.LogInformation("Updated journey {JourneyId} EndFuel to {FuelLevel}",
                        stop.Journey.Id, stop.Journey.EndFuel);
                }

                if (!string.IsNullOrEmpty(request.VehicleCondition))
                {
                    stop.Journey.VehicleCondition = request.VehicleCondition;
                    _logger.LogInformation("Updated journey {JourneyId} VehicleCondition to {Condition}",
                        stop.Journey.Id, stop.Journey.VehicleCondition);
                }

                // Not: FuelLevel ve VehicleCondition artık Journey entity'sinde de saklanıyor
                if (!string.IsNullOrEmpty(request.Notes))
                {
                    var additionalInfo = new List<string>();
                    if (!string.IsNullOrEmpty(request.FuelLevel))
                        additionalInfo.Add($"Yakıt Seviyesi: {TranslateFuelLevel(request.FuelLevel)}");
                    if (!string.IsNullOrEmpty(request.VehicleCondition))
                        additionalInfo.Add($"Araç Durumu: {TranslateVehicleCondition(request.VehicleCondition)}");

                    if (additionalInfo.Any())
                    {
                        request.Notes = request.Notes + "\n" + string.Join(", ", additionalInfo);
                    }
                }
            }
            else
            {
                _logger.LogWarning("Depot return completed but journey {JourneyId} has no vehicle assigned", request.JourneyId);
            }
        }

        // YENİ: Çoklu fotoğraf için kontrol
        var allPhotoFiles = new List<IFormFile>();
        
        // Eski API uyumluluğu için tek PhotoFile kontrolü
        if (request.PhotoFile != null && request.PhotoFile.Length > 0)
        {
            allPhotoFiles.Add(request.PhotoFile);
        }
        
        // Yeni çoklu fotoğraf desteği
        if (request.PhotoFiles != null && request.PhotoFiles.Any())
        {
            allPhotoFiles.AddRange(request.PhotoFiles);
        }

        // Status Completed ise proof kontrolü
        if (request.Status == JourneyStopStatus.Completed)
        {
            // ✅ YENİ: ReceiverName kontrolü
            if (string.IsNullOrWhiteSpace(request.ReceiverName))
            {
                throw new ApiException("Teslim alan kişinin adı zorunludur.", 400);
            }
            
            // İmza kontrolü
            if (stop.RouteStop.SignatureRequired && request.SignatureFile == null)
            {
                throw new ApiException("Bu teslimat için imza zorunludur.", 400);
            }
            
            // Fotoğraf kontrolü - en az bir fotoğraf olmalı
            if (stop.RouteStop.PhotoRequired && !allPhotoFiles.Any())
            {
                throw new ApiException("Bu teslimat için en az bir fotoğraf zorunludur.", 400);
            }
            
            // ProofOfDeliveryRequired kontrolü (ikisi birden gerekli)
            if (stop.RouteStop.ProofOfDeliveryRequired && 
                (request.SignatureFile == null || !allPhotoFiles.Any()))
            {
                throw new ApiException("Bu teslimat için hem imza hem fotoğraf zorunludur.", 400);
            }
        }

        // Maksimum fotoğraf sayısı kontrolü
        if (allPhotoFiles.Count > 10)
        {
            throw new ApiException("En fazla 10 fotoğraf yükleyebilirsiniz.", 400);
        }

        var workspace = await _context.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == User.WorkspaceId, cancellationToken);

        var previousStatus = stop.Status;

        // BUGFIX S3.3: Validate status transitions - prevent invalid state changes
        if (!IsValidStatusTransition(previousStatus, request.Status))
        {
            throw new ApiException(
                $"Geçersiz durum geçişi: {previousStatus} -> {request.Status}. " +
                $"Tamamlanmış veya başarısız bir durak tekrar bekliyor durumuna alınamaz.",
                400);
        }

        stop.Status = request.Status;
        stop.CheckInTime = request.CheckInTime;
        stop.CheckOutTime = request.CheckOutTime;
        stop.UpdatedAt = DateTime.UtcNow;

        // ✅ YENİ: CheckIn yapıldığında gecikme hesapla
        bool requiresDelayReason = false;
        int newDelay = 0;
        int cumulativeDelay = 0;

        if (request.Status == JourneyStopStatus.InProgress && request.CheckInTime.HasValue)
        {
            // Türkiye timezone
            var turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");

            // Önceki durakların kümülatif gecikmesini hesapla
            var previousStops = await _context.JourneyStops
                .Where(s => s.JourneyId == stop.JourneyId && s.Order < stop.Order && s.CheckInTime.HasValue)
                .OrderByDescending(s => s.Order)
                .ToListAsync(cancellationToken);

            int previousCumulativeDelay = 0;
            if (previousStops.Any())
            {
                var previousStop = previousStops.First();
                previousCumulativeDelay = previousStop.CumulativeDelay;
            }

            // Mevcut durağın gerçek gecikmesini hesapla
            var actualDelay = stop.CalculateActualDelay(turkeyTimeZone);

            // Yeni gecikmeyi hesapla
            newDelay = actualDelay - previousCumulativeDelay;

            // Kümülatif gecikmeyi hesapla
            cumulativeDelay = actualDelay;

            // NewDelay ve CumulativeDelay'i kaydet
            stop.NewDelay = newDelay;
            stop.CumulativeDelay = cumulativeDelay;

            // Eğer yeni gecikme 15 dakikadan fazla ise, sebep sor
            if (newDelay >= 15)
            {
                requiresDelayReason = true;
                _logger.LogInformation(
                    "Stop {StopId} has {NewDelay} minutes new delay. Delay reason required.",
                    stop.Id, newDelay);
            }
        }

        // ✅ Update LastDeliveryDate when stop is completed or failed
        if ((request.Status == JourneyStopStatus.Completed || request.Status == JourneyStopStatus.Failed) &&
            stop.RouteStop?.Customer != null)
        {
            var customer = stop.RouteStop.Customer;
            var now = DateTime.UtcNow;

            // Update customer's LastDeliveryDate if this is newer or doesn't exist
            if (!customer.LastDeliveryDate.HasValue || customer.LastDeliveryDate.Value < now)
            {
                customer.LastDeliveryDate = now;
                _logger.LogInformation("Updated LastDeliveryDate for customer {CustomerId} to {Date}",
                    customer.Id, now);
            }
        }

        string? signatureUrl = null;
        string? photoUrl = null;
        List<JourneyStopPhoto> additionalPhotos = new();
        
        var storageMode = _configuration.GetValue<string>("Storage:Mode") ?? "Local";
        var useCloudinary = storageMode == "Cloudinary";

        // İmza yükleme
        if (request.SignatureFile != null && request.SignatureFile.Length > 0)
        {
            try
            {
                if (useCloudinary)
                {
                    using var stream = request.SignatureFile.OpenReadStream();
                    var folder = $"journeys/{request.JourneyId}/stop-{request.StopId}";
                    var publicId = $"signature_{DateTime.UtcNow:yyyyMMddHHmmss}";
                    
                    var uploadResult = await _cloudinaryService.UploadAsync(
                        stream, 
                        folder, 
                        publicId, 
                        "image");
                    
                    if (uploadResult.Success)
                    {
                        signatureUrl = uploadResult.SecureUrl;
                        _logger.LogInformation("Signature uploaded to Cloudinary: {Url}", signatureUrl);
                    }
                    else
                    {
                        _logger.LogError("Failed to upload signature to Cloudinary: {Error}", uploadResult.Error);
                        using var fallbackStream = request.SignatureFile.OpenReadStream();
                        signatureUrl = await _blobStorage.UploadAsync(
                            fallbackStream, 
                            $"journey-{request.JourneyId}/stop-{request.StopId}/signature_{DateTime.UtcNow:yyyyMMddHHmmss}.png",
                            request.SignatureFile.ContentType ?? "image/png");
                    }
                }
                else
                {
                    using var stream = request.SignatureFile.OpenReadStream();
                    signatureUrl = await _blobStorage.UploadAsync(
                        stream, 
                        $"journey-{request.JourneyId}/stop-{request.StopId}/signature_{DateTime.UtcNow:yyyyMMddHHmmss}.png",
                        request.SignatureFile.ContentType ?? "image/png");
                    
                    _logger.LogInformation("Signature uploaded to blob storage: {Url}", signatureUrl);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload signature for stop {StopId}", request.StopId);
            }
        }

        // YENİ: Çoklu fotoğraf yükleme
        for (int i = 0; i < allPhotoFiles.Count; i++)
        {
            var photoFile = allPhotoFiles[i];
            if (photoFile.Length == 0) continue;
            
            try
            {
                string uploadedPhotoUrl = null;
                string thumbnailUrl = null;
                
                if (useCloudinary)
                {
                    using var stream = photoFile.OpenReadStream();
                    var folder = $"journeys/{request.JourneyId}/stop-{request.StopId}";
                    var publicId = $"photo_{DateTime.UtcNow:yyyyMMddHHmmss}_{i}";
                    
                    var uploadResult = await _cloudinaryService.UploadAsync(
                        stream, 
                        folder, 
                        publicId, 
                        "image");
                    
                    if (uploadResult.Success)
                    {
                        uploadedPhotoUrl = uploadResult.SecureUrl;
                        // Thumbnail URL oluştur
                        thumbnailUrl = _cloudinaryService.GetThumbnailUrl(uploadResult.PublicId, 300, 300);
                        _logger.LogInformation("Photo {Index} uploaded to Cloudinary: {Url}", i, uploadedPhotoUrl);
                    }
                    else
                    {
                        _logger.LogError("Failed to upload photo {Index} to Cloudinary: {Error}", i, uploadResult.Error);
                        using var fallbackStream = photoFile.OpenReadStream();
                        uploadedPhotoUrl = await _blobStorage.UploadAsync(
                            fallbackStream,
                            $"journey-{request.JourneyId}/stop-{request.StopId}/photo_{DateTime.UtcNow:yyyyMMddHHmmss}_{i}.jpg",
                            photoFile.ContentType ?? "image/jpeg");
                        thumbnailUrl = uploadedPhotoUrl; // Blob storage için aynı URL
                    }
                }
                else
                {
                    using var stream = photoFile.OpenReadStream();
                    uploadedPhotoUrl = await _blobStorage.UploadAsync(
                        stream,
                        $"journey-{request.JourneyId}/stop-{request.StopId}/photo_{DateTime.UtcNow:yyyyMMddHHmmss}_{i}.jpg",
                        photoFile.ContentType ?? "image/jpeg");
                    thumbnailUrl = uploadedPhotoUrl; // Blob storage için aynı URL
                    
                    _logger.LogInformation("Photo {Index} uploaded to blob storage: {Url}", i, uploadedPhotoUrl);
                }
                
                // İlk fotoğrafı ana PhotoUrl'e kaydet (backward compatibility)
                if (i == 0)
                {
                    photoUrl = uploadedPhotoUrl;
                }
                
                // Tüm fotoğrafları (ilki dahil) JourneyStopPhotos tablosuna ekle
                if (!string.IsNullOrEmpty(uploadedPhotoUrl))
                {
                    additionalPhotos.Add(new JourneyStopPhoto
                    {
                        JourneyId = request.JourneyId,
                        StopId = request.StopId,
                        PhotoUrl = uploadedPhotoUrl,
                        ThumbnailUrl = thumbnailUrl ?? uploadedPhotoUrl,
                        DisplayOrder = i,
                        ReceiverName = request.ReceiverName, // ✅ YENİ EKLENEN
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload photo {Index} for stop {StopId}", i, request.StopId);
            }
        }

        var journeyStatus = new JourneyStatus(
            stop.JourneyId,
            stop.Id,
            request.Status switch
            {
                JourneyStopStatus.InProgress => JourneyStatusType.Arrived,
                JourneyStopStatus.Completed => JourneyStatusType.Completed,
                JourneyStopStatus.Failed => JourneyStatusType.Cancelled,
                _ => JourneyStatusType.Processing
            })
        {
            Notes = request.Notes,
            FailureReason = request.FailureReason,
            ReceiverName = request.ReceiverName, // ✅ YENİ EKLENEN
            SignatureUrl = signatureUrl,
            PhotoUrl = photoUrl, // İlk fotoğraf backward compatibility için
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.JourneyStatuses.Add(journeyStatus);
        
        // YENİ: Ek fotoğrafları kaydet
        if (additionalPhotos.Any())
        {
            _context.JourneyStopPhotos.AddRange(additionalPhotos);
        }

        // CustomerFeedback token oluştur (Completed için)
        string feedbackToken = null;
        if (request.Status == JourneyStopStatus.Completed && stop.RouteStop?.Customer != null)
        {
            feedbackToken = _feedbackService.GenerateFeedbackToken(
                request.JourneyId,
                request.StopId,
                stop.RouteStop.CustomerId.Value);

            // Stub feedback kaydı oluştur (müşteri değerlendirme yapmazsa bile takip edebilmek için)
            var existingFeedback = await _context.CustomerFeedback
                .FirstOrDefaultAsync(f => f.JourneyId == request.JourneyId && f.JourneyStopId == request.StopId);

            if (existingFeedback == null)
            {
                var feedbackStub = new CustomerFeedback
                {
                    WorkspaceId = User.WorkspaceId,
                    JourneyId = request.JourneyId,
                    JourneyStopId = request.StopId,
                    CustomerId = stop.RouteStop.CustomerId.Value,
                    FeedbackToken = feedbackToken,
                    OverallRating = 1, // Stub olarak 1, gerçek submit'te güncellenir
                    Comments = "[STUB]", // Stub marker
                    SubmittedAt = DateTime.MinValue // Henüz submit edilmemiş
                };

                _context.CustomerFeedback.Add(feedbackStub);
            }
        }

        if (request.Status == JourneyStopStatus.Completed || request.Status == JourneyStopStatus.Failed)
        {
            var journey = stop.Journey;
            var allStops = await _context.JourneyStops
                .Where(s => s.JourneyId == journey.Id)
                .OrderBy(s => s.Order)
                .ToListAsync(cancellationToken);

            // ✅ DELTA-BASED ETA UPDATE
            // Gecikme deltaları hesapla: Planned Departure Time vs Actual Completion Time
            if (stop.EstimatedDepartureTime.HasValue)
            {
                // ✅ Türkiye saatini al (server UTC'de olabilir)
                var turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
                var turkeyNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, turkeyTimeZone);

                // BUGFIX S3.2: Handle midnight crossing properly
                // Calculate planned departure as DateTime (not just TimeSpan)
                var journeyDate = journey.Date.Date;
                var plannedDepartureDateTime = journeyDate.Add(stop.EstimatedDepartureTime.Value);

                // If planned time is early morning (00:00-06:00), check if it should be next day
                // by comparing with previous stops
                if (stop.EstimatedDepartureTime.Value.TotalHours < 6)
                {
                    var previousStops = allStops.Where(s => s.Order < stop.Order).OrderBy(s => s.Order).ToList();
                    var hasPreviousLateStop = previousStops.Any(s => s.EstimatedDepartureTime.HasValue &&
                                                                     s.EstimatedDepartureTime.Value.TotalHours > 18);
                    if (hasPreviousLateStop)
                    {
                        plannedDepartureDateTime = journeyDate.AddDays(1).Add(stop.EstimatedDepartureTime.Value);
                        _logger.LogInformation($"[ETA_UPDATE] BUGFIX S3.2: Stop #{stop.Id} crosses midnight. Planned: {plannedDepartureDateTime:HH:mm} (next day)");
                    }
                }

                var actualCompletionDateTime = request.CheckOutTime ?? turkeyNow;
                var delay = actualCompletionDateTime - plannedDepartureDateTime;

                if (Math.Abs(delay.TotalMinutes) >= 5) // 5 dakika veya daha fazla sapma varsa
                {
                    _logger.LogInformation($"[ETA_UPDATE] Stop #{stop.Id} completed with {delay.TotalMinutes:F1} minute delay. Updating remaining stops.");

                    // Sonraki bekleyen tüm durakların ETA'larını güncelle
                    var remainingStops = allStops
                        .Where(s => s.Status == JourneyStopStatus.Pending && s.Order > stop.Order)
                        .ToList();

                    foreach (var remainingStop in remainingStops)
                    {
                        // BUGFIX S3.16: Save original ETA (first time only)
                        if (remainingStop.OriginalEstimatedArrivalTime == TimeSpan.Zero)
                        {
                            remainingStop.OriginalEstimatedArrivalTime = remainingStop.EstimatedArrivalTime;
                            remainingStop.OriginalEstimatedDepartureTime = remainingStop.EstimatedDepartureTime ?? TimeSpan.Zero;
                            _logger.LogInformation($"[ETA_UPDATE] BUGFIX S3.16: Saved original ETA for stop #{remainingStop.Id}: Arrival={remainingStop.OriginalEstimatedArrivalTime}, Departure={remainingStop.OriginalEstimatedDepartureTime}");
                        }

                        // Current ETA'ları güncelle (delay delta ekle)
                        var newArrivalTime = remainingStop.EstimatedArrivalTime + delay;

                        // BUGFIX S3.2: Handle negative TimeSpan (midnight crossing)
                        if (newArrivalTime.TotalSeconds < 0)
                        {
                            newArrivalTime = TimeSpan.FromDays(1) + newArrivalTime; // Add 24 hours
                            _logger.LogInformation($"[ETA_UPDATE] BUGFIX S3.2: Stop #{remainingStop.Id} negative time fixed (midnight crossing)");
                        }

                        // ✅ SQL Server Time tipi maksimum 23:59:59.9999999 değerini alabilir
                        // 24 saati aşan değerleri kontrol et
                        if (newArrivalTime.TotalHours >= 24)
                        {
                            // 24 saati aşarsa, TimeSpan.MaxValue yerine 23:59:59 kullan
                            remainingStop.EstimatedArrivalTime = new TimeSpan(23, 59, 59);
                            _logger.LogWarning($"[ETA_UPDATE] Stop #{remainingStop.Id} ETA exceeded 24 hours ({newArrivalTime.TotalHours:F1}h). Capped at 23:59:59.");
                        }
                        else
                        {
                            remainingStop.EstimatedArrivalTime = newArrivalTime;
                        }

                        if (remainingStop.EstimatedDepartureTime.HasValue)
                        {
                            var newDepartureTime = remainingStop.EstimatedDepartureTime.Value + delay;

                            // BUGFIX S3.2: Handle negative TimeSpan
                            if (newDepartureTime.TotalSeconds < 0)
                            {
                                newDepartureTime = TimeSpan.FromDays(1) + newDepartureTime;
                            }

                            if (newDepartureTime.TotalHours >= 24)
                            {
                                remainingStop.EstimatedDepartureTime = new TimeSpan(23, 59, 59);
                                _logger.LogWarning($"[ETA_UPDATE] Stop #{remainingStop.Id} departure time exceeded 24 hours. Capped at 23:59:59.");
                            }
                            else
                            {
                                remainingStop.EstimatedDepartureTime = newDepartureTime;
                            }
                        }

                        remainingStop.UpdatedAt = DateTime.UtcNow;
                    }

                    _logger.LogInformation($"[ETA_UPDATE] Updated {remainingStops.Count} remaining stops with {delay.TotalMinutes:F1} minute delta.");
                }
                else
                {
                    _logger.LogInformation($"[ETA_UPDATE] Stop #{stop.Id} completed with minor delay ({delay.TotalMinutes:F1} min). No ETA update needed (threshold: 5 min).");
                }
            }

            var nextPendingStop = allStops
                .FirstOrDefault(s => s.Status == JourneyStopStatus.Pending && s.Order > stop.Order);

            if (nextPendingStop != null)
            {
                journey.CurrentStopIndex = allStops.IndexOf(nextPendingStop);
            }
            else
            {
                var allCompleted = allStops.All(s =>
                    s.Status == JourneyStopStatus.Completed ||
                    s.Status == JourneyStopStatus.Failed ||
                    s.Status == JourneyStopStatus.Skipped);

                if (allCompleted)
                {
                    journey.Finish();
                    _logger.LogInformation("Journey {JourneyId} completed", journey.Id);

                    // ✅ YENİ: Gecikme kontrolü ve rapor gönderme
                    await CheckAndSendDelayReportAsync(journey, cancellationToken);
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        // ✅ BUGFIX: CheckOut yapıldığında kalan durakların ETA'larını güncelle
        var isCheckOut = request.CheckOutTime.HasValue &&
                         (request.Status == JourneyStopStatus.Completed || request.Status == JourneyStopStatus.Failed);

        if (isCheckOut && !isDepotReturn)
        {
            _logger.LogInformation("Stop {StopId} completed, recalculating ETAs for remaining stops", request.StopId);
            try
            {
                var optimizeCommand = new OptimizeRouteCommand
                {
                    RouteId = stop.Journey.RouteId,
                    PreserveOrder = true, // Sıralamayı koruyarak sadece ETA hesapla
                    IsTimeDeviationOptimization = true, // Yetki kontrolünü atla
                    AuthenticatedUserId = request.AuthenticatedUserId
                };

                // ETA'ları güncelleyecek - şu anki zamanı kullanarak kalan durakları hesaplar
                await _sender.Send(optimizeCommand, cancellationToken);

                _logger.LogInformation("ETAs recalculated successfully after stop {StopId} completion", request.StopId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to recalculate ETAs after stop {StopId} completion", request.StopId);
                // ETA hesaplaması başarısız olsa bile durak güncellemesine devam et
            }
        }

        // SignalR Notifications - YENİ: photoUrls array ve receiverName ekle
        try
        {
            var photoUrls = additionalPhotos.Select(p => p.PhotoUrl).ToList();
            
            await _hubContext.Clients.Group($"journey-{request.JourneyId}")
                .SendAsync("StopUpdated", request.JourneyId, request.StopId, new
                {
                    stopId = stop.Id,
                    status = stop.Status.ToString(),
                    checkInTime = stop.CheckInTime,
                    checkOutTime = stop.CheckOutTime,
                    signatureUrl = signatureUrl,
                    photoUrl = photoUrl,
                    photoUrls = photoUrls, // YENİ: Tüm fotoğraf URL'leri
                    receiverName = request.ReceiverName, // ✅ YENİ EKLENEN
                    notes = request.Notes,
                    failureReason = request.FailureReason,
                    // ✅ YENİ: Gecikme bilgileri
                    requiresDelayReason = requiresDelayReason,
                    newDelay = newDelay,
                    cumulativeDelay = cumulativeDelay
                }, cancellationToken);

            await _hubContext.Clients.Group($"workspace-{User.WorkspaceId}")
                .SendAsync("journeyupdated", request.JourneyId, cancellationToken);

            _logger.LogInformation("SignalR notifications sent for journey {JourneyId}, stop {StopId}", 
                request.JourneyId, request.StopId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SignalR notifications");
        }

        // Email ve WhatsApp notification for completed deliveries
        if (request.Status == JourneyStopStatus.Completed && stop.RouteStop?.Customer != null)
        {
            var customer = stop.RouteStop.Customer;
            var driver = stop.Journey.Driver;
            var vehicle = stop.Journey.Vehicle;
            
            var trackingToken = GenerateTrackingToken(request.JourneyId, request.StopId);
            var baseUrl = _configuration["Tracking:ApiBaseUrl"] ?? "https://api.yolpilot.com";
            var trackingUrl = $"{baseUrl}/tracking/delivery/{request.JourneyId}/{request.StopId}?token={trackingToken}";
            var frontendUrl = _configuration["Tracking:BaseUrl"] ?? "https://app.yolpilot.com"; 
            var feedbackUrl = $"{frontendUrl}/feedback/{feedbackToken}";

            // Template data hazırla - ✅ ReceiverName eklendi
            var templateData = new Dictionary<string, object>
            {
                ["customer"] = new 
                { 
                    name = customer.Name,
                    address = customer.Address,
                    email = customer.Email,
                    phone = customer.Phone
                },
                ["completedTime"] = DateTime.Now.ToString("HH:mm"),
                ["receiverName"] = request.ReceiverName ?? "", // ✅ YENİ EKLENEN
                ["driver"] = new 
                { 
                    name = driver?.Name ?? "Teslimat Görevlisi",
                    phone = driver?.Phone ?? workspace.PhoneNumber
                },
                ["vehicle"] = vehicle != null ? new
                {
                    brand = vehicle.Brand,
                    model = vehicle.Model,
                    plateNumber = vehicle.PlateNumber
                } : null,
                ["signatureUrl"] = signatureUrl ?? $"{trackingUrl}/signature",
                ["photoUrl"] = photoUrl ?? $"{trackingUrl}/photo",
                ["stop"] = new { notes = request.Notes ?? "" },
                ["feedbackUrl"] = feedbackUrl,
                ["trackingUrl"] = trackingUrl,
                ["workspace"] = new
                {
                    name = workspace.Name,
                    email = workspace.Email,
                    phoneNumber = workspace.PhoneNumber
                },
                ["currentDate"] = DateTime.Now.ToString("dd MMMM yyyy"),
                ["currentTime"] = DateTime.Now.ToString("HH:mm")
            };

            // BUGFIX S3.12: Email template fallback - always send email even if template fails
            if (!string.IsNullOrEmpty(customer.Email))
            {
                try
                {
                    var (subject, body) = await _templateService.GetProcessedEmailContentAsync(
                        workspace.Id,
                        TemplateType.DeliveryCompleted,
                        templateData);

                    if (!string.IsNullOrEmpty(subject) && !string.IsNullOrEmpty(body))
                    {
                        await _emailService.SendDeliveryCompletedToCustomerContactsAsync(
                            customer.Id,
                            customer.Name,
                            trackingUrl,
                            request.Notes,
                            request.ReceiverName,
                            feedbackUrl);
                        _logger.LogInformation($"BUGFIX S3.12: Delivery completed email sent to {customer.Email} using custom template");
                    }
                    else
                    {
                        // BUGFIX S3.12: Template is invalid, send fallback email
                        _logger.LogWarning($"BUGFIX S3.12: Delivery completed email template is invalid for {customer.Email}, sending fallback email");
                        await _emailService.SendDeliveryCompletedToCustomerContactsAsync(
                            customer.Id,
                            customer.Name,
                            trackingUrl,
                            request.Notes,
                            request.ReceiverName,
                            feedbackUrl);
                    }
                }
                catch (Exception ex)
                {
                    // BUGFIX S3.12: Even if template processing fails, send fallback email
                    _logger.LogError(ex, "BUGFIX S3.12: Failed to process email template, sending fallback email");
                    try
                    {
                        await _emailService.SendDeliveryCompletedToCustomerContactsAsync(
                            customer.Id,
                            customer.Name,
                            trackingUrl,
                            request.Notes,
                            request.ReceiverName,
                            feedbackUrl);
                        _logger.LogInformation($"BUGFIX S3.12: Fallback email sent successfully to {customer.Email}");
                    }
                    catch (Exception fallbackEx)
                    {
                        _logger.LogError(fallbackEx, "BUGFIX S3.12: Fallback email also failed");
                    }
                }
            }

            // WhatsApp gönder
            bool sendWhatsApp = workspace.WhatsAppMode != WhatsAppMode.Disabled && 
                               workspace.WhatsAppNotifyCompletion;
            
            if (sendWhatsApp)
            {
                var canSendWhatsApp = await _subscriptionService.CanSendWhatsApp(User.WorkspaceId);
                sendWhatsApp = canSendWhatsApp;
            }

            if (sendWhatsApp && customer.WhatsAppOptIn && !string.IsNullOrEmpty(customer.WhatsApp))
            {
                try
                {
                    // ✅ DÜZELTİLDİ: receiverName değişkeni tanımlandı
                    var receiverName = request.ReceiverName ?? "";
                    
                    var success = await _whatsAppService.SendDeliveryCompletedMessage(
                        workspace,
                        customer.WhatsApp,
                        customer.Name,
                        receiverName,  // Artık tanımlı
                        trackingUrl,
                        feedbackUrl);

                    if (success)
                    {
                        _logger.LogInformation($"Delivery completed WhatsApp sent to {customer.Name} ({customer.WhatsApp})");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send WhatsApp to {customer.WhatsApp}");
                }
            }
        }

        return true;
    }

    private string GenerateTrackingToken(int journeyId, int stopId)
    {
        var secret = _configuration["Tracking:Secret"] ?? "YolPilot2024Secret!";
        var workspaceId = User.WorkspaceId;
        var input = $"{journeyId}-{stopId}-{workspaceId}-{secret}";
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hash).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private string TranslateFuelLevel(string fuelLevel)
    {
        return fuelLevel switch
        {
            "full" => "Dolu",
            "three_quarters" => "3/4",
            "half" => "1/2",
            "quarter" => "1/4",
            "empty" => "Boş",
            _ => fuelLevel
        };
    }

    private string TranslateVehicleCondition(string condition)
    {
        return condition switch
        {
            "good" => "İyi",
            "needs_cleaning" => "Temizlik Gerekli",
            "needs_maintenance" => "Bakım Gerekli",
            "damaged" => "Hasarlı",
            _ => condition
        };
    }

    // BUGFIX S3.3: Validate status transitions
    private static bool IsValidStatusTransition(JourneyStopStatus from, JourneyStopStatus to)
    {
        // Same status is always allowed
        if (from == to) return true;

        // Valid transitions:
        // Pending -> InProgress, Completed, Failed, Skipped
        // InProgress -> Completed, Failed, Skipped
        // Completed/Failed/Skipped -> Cannot go back

        return from switch
        {
            JourneyStopStatus.Pending => to is JourneyStopStatus.InProgress or JourneyStopStatus.Completed
                                             or JourneyStopStatus.Failed or JourneyStopStatus.Skipped,
            JourneyStopStatus.InProgress => to is JourneyStopStatus.Completed or JourneyStopStatus.Failed
                                                or JourneyStopStatus.Skipped,
            JourneyStopStatus.Completed => false, // Cannot change from completed
            JourneyStopStatus.Failed => false,    // Cannot change from failed
            JourneyStopStatus.Skipped => false,   // Cannot change from skipped
            _ => false
        };
    }

    /// <summary>
    /// Seferin gecikmesini kontrol eder ve gerekirse yönetici emaillerine gecikme raporu gönderir
    /// </summary>
    private async Task CheckAndSendDelayReportAsync(Journey journey, CancellationToken cancellationToken)
    {
        try
        {
            Console.WriteLine($"[DELAY_ALERT] Starting delay check for Journey #{journey.Id}");

            // Workspace bilgilerini al
            var workspace = await _context.Workspaces
                .FirstOrDefaultAsync(w => w.Id == User.WorkspaceId, cancellationToken);

            Console.WriteLine($"[DELAY_ALERT] Workspace found: {workspace?.Name}, Settings: {workspace?.Settings != null}, DelayAlertSettings: {workspace?.Settings?.DelayAlertSettings != null}");

            if (workspace?.Settings?.DelayAlertSettings == null ||
                !workspace.Settings.DelayAlertSettings.Enabled)
            {
                // Gecikme uyarıları devre dışı
                Console.WriteLine($"[DELAY_ALERT] Delay alerts disabled or not configured. Enabled: {workspace?.Settings?.DelayAlertSettings?.Enabled}");
                return;
            }

            var delaySettings = workspace.Settings.DelayAlertSettings;
            var alertEmails = delaySettings.GetEmailList();

            Console.WriteLine($"[DELAY_ALERT] Alert emails count: {alertEmails.Count}, Emails: {string.Join(", ", alertEmails)}");

            if (!alertEmails.Any())
            {
                // Email adresi yok
                Console.WriteLine($"[DELAY_ALERT] No alert emails configured");
                return;
            }

            // Journey'nin stops'ları yükle (eğer yüklü değilse)
            if (journey.Stops == null || !journey.Stops.Any())
            {
                await _context.Entry(journey)
                    .Collection(j => j.Stops)
                    .LoadAsync(cancellationToken);
            }

            // Route bilgilerini yükle (mesafe için)
            if (journey.Route == null && journey.RouteId > 0)
            {
                await _context.Entry(journey)
                    .Reference(j => j.Route)
                    .LoadAsync(cancellationToken);
            }

            // Planlanan tamamlanma zamanını hesapla
            // Son stop'un estimated arrival time + service time kullanılır
            var lastStop = journey.Stops?.OrderBy(s => s.Order).LastOrDefault();
            if (lastStop == null || journey.Date == null || journey.FinishedAt == null)
            {
                // Bilgiler eksik, kontrol yapılamaz
                Console.WriteLine($"[DELAY_ALERT] Missing journey info - LastStop: {lastStop != null}, Date: {journey.Date != null}, FinishedAt: {journey.FinishedAt != null}");
                return;
            }

            // Kilometre bilgilerini hesapla
            decimal? plannedDistanceKm = null;
            if (journey.Route?.TotalDistance != null)
            {
                plannedDistanceKm = (decimal)journey.Route.TotalDistance.Value;
            }
            decimal? actualDistanceKm = null;
            if (journey.StartKm.HasValue && journey.EndKm.HasValue)
            {
                actualDistanceKm = journey.EndKm.Value - journey.StartKm.Value;
            }

            Console.WriteLine($"[DELAY_ALERT] Distance - Planned: {plannedDistanceKm?.ToString("F1") ?? "N/A"} km, Actual: {actualDistanceKm?.ToString("F1") ?? "N/A"} km");

            var plannedCompletionTime = journey.Date.Date.Add(lastStop.EstimatedArrivalTime);

            // Eğer EstimatedDepartureTime varsa onu kullan (service time dahil)
            if (lastStop.EstimatedDepartureTime.HasValue)
            {
                plannedCompletionTime = journey.Date.Date.Add(lastStop.EstimatedDepartureTime.Value);
            }

            var actualCompletionTime = journey.FinishedAt.Value;
            var delay = actualCompletionTime - plannedCompletionTime;

            Console.WriteLine($"[DELAY_ALERT] Planned: {plannedCompletionTime:yyyy-MM-dd HH:mm:ss}, Actual: {actualCompletionTime:yyyy-MM-dd HH:mm:ss}, Delay: {delay.TotalHours:F2} hours ({delay.TotalMinutes:F0} minutes), Threshold: {delaySettings.ThresholdHours} hours");

            // Gecikme threshold'unu kontrol et
            if (delay.TotalHours >= delaySettings.ThresholdHours)
            {
                Console.WriteLine($"[DELAY_ALERT] Delay threshold exceeded! Sending email to {alertEmails.Count} recipient(s)");

                // Driver bilgisini yükle
                if (journey.Driver == null && journey.DriverId > 0)
                {
                    await _context.Entry(journey)
                        .Reference(j => j.Driver)
                        .LoadAsync(cancellationToken);
                }

                // Gecikme var ve threshold'u aştı - email gönder
                await _emailService.SendJourneyDelayReportAsync(
                    recipients: alertEmails,
                    journeyId: journey.Id,
                    journeyName: journey.Name,
                    driverName: journey.Driver?.Name ?? "Bilinmeyen Sürücü",
                    plannedCompletionTime: plannedCompletionTime,
                    actualCompletionTime: actualCompletionTime,
                    delayDuration: delay,
                    workspaceName: workspace.Name,
                    plannedDistanceKm: plannedDistanceKm,
                    actualDistanceKm: actualDistanceKm
                );

                Console.WriteLine($"[DELAY_ALERT] Email sent successfully");
            }
            else
            {
                Console.WriteLine($"[DELAY_ALERT] Delay {delay.TotalHours:F2}h is below threshold {delaySettings.ThresholdHours}h, no email sent");
            }
        }
        catch (Exception ex)
        {
            // Gecikme raporu gönderilirken hata olsa bile journey completion başarılı olmalı
            // Sadece log'la
            Console.WriteLine($"[DELAY_ALERT] Error checking/sending delay report for Journey #{journey.Id}: {ex.Message}");
            Console.WriteLine($"[DELAY_ALERT] Stack trace: {ex.StackTrace}");
        }
    }
}

public class CheckInJourneyStopCommand : BaseAuthenticatedCommand<Responses.Journeys.CheckInResponse>
{
    [JsonIgnore] public override bool RequiresDriver => true;
    [JsonIgnore] public override bool RequiresWorkspaceAccess => true;
    [JsonIgnore] public int JourneyId { get; set; }
    [JsonIgnore] public int StopId { get; set; }
}

public class CheckInJourneyStopCommandHandler : BaseAuthenticatedCommandHandler<CheckInJourneyStopCommand, Responses.Journeys.CheckInResponse>
{
    private readonly IMediator _mediator;
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IWhatsAppService _whatsAppService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ITemplateService _templateService;
    private readonly ILogger<CheckInJourneyStopCommandHandler> _logger;

    public CheckInJourneyStopCommandHandler(
        IUserService userService, 
        IMediator mediator,
        AppDbContext context,
        IEmailService emailService,
        IWhatsAppService whatsAppService,
        ISubscriptionService subscriptionService,
        ITemplateService templateService,
        ILogger<CheckInJourneyStopCommandHandler> logger)
        : base(userService)
    {
        _mediator = mediator;
        _context = context;
        _emailService = emailService;
        _whatsAppService = whatsAppService;
        _subscriptionService = subscriptionService;
        _templateService = templateService;
        _logger = logger;
    }

    protected override async Task<Responses.Journeys.CheckInResponse> HandleCommand(CheckInJourneyStopCommand request, CancellationToken cancellationToken)
    {
        var currentStop = await _context.JourneyStops
            .Include(s => s.RouteStop)
                .ThenInclude(rs => rs.Customer)
            .Include(s => s.Journey)
                .ThenInclude(j => j.Driver)
            .Include(s => s.Journey)
                .ThenInclude(j => j.Vehicle)
            .Include(s => s.Journey)
                .ThenInclude(j => j.Stops)
            .FirstOrDefaultAsync(s => 
                s.Id == request.StopId && 
                s.JourneyId == request.JourneyId,
                cancellationToken);

        if (currentStop != null)
        {
            var workspace = await _context.Workspaces
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == User.WorkspaceId, cancellationToken);

            var allStops = await _context.JourneyStops
                .Include(s => s.RouteStop)
                    .ThenInclude(rs => rs.Customer)
                .Where(s => s.JourneyId == request.JourneyId)
                .OrderBy(s => s.Order)
                .ToListAsync(cancellationToken);

            var nextStop = allStops.FirstOrDefault(s => s.Order == currentStop.Order + 1);
            
            if (nextStop?.RouteStop?.Customer != null)
            {
                var customer = nextStop.RouteStop.Customer;
                var driver = currentStop.Journey.Driver;
                
                // ETA hesaplama
                string minTimeStr, maxTimeStr;
                var currentServiceTime = currentStop.RouteStop?.ServiceTime ?? TimeSpan.FromMinutes(10);
                var currentServiceMinutes = (int)currentServiceTime.TotalMinutes;
                
                var travelMinutes = 0;
                if (nextStop.EstimatedArrivalTime != TimeSpan.Zero && currentStop.EstimatedDepartureTime.HasValue)
                {
                    var travelTime = nextStop.EstimatedArrivalTime - currentStop.EstimatedDepartureTime.Value;
                    travelMinutes = (int)travelTime.TotalMinutes;
                }
                else
                {
                    var distanceKm = nextStop.Distance;
                    travelMinutes = (int)(distanceKm / 40.0 * 60);
                }
                
                var totalMinutes = currentServiceMinutes + travelMinutes;
                var roundedMinutes = (int)(Math.Ceiling(totalMinutes / 5.0) * 5);
                
                var minTime = roundedMinutes;
                var maxTime = roundedMinutes + 30;
                
                if (minTime < 60)
                    minTimeStr = $"{minTime} dakika";
                else
                {
                    var hours = minTime / 60;
                    var minutes = minTime % 60;
                    minTimeStr = minutes == 0 ? $"{hours} saat" : $"{hours} saat {minutes} dakika";
                }
                
                if (maxTime < 60)
                    maxTimeStr = $"{maxTime} dakika";
                else
                {
                    var hours = maxTime / 60;
                    var minutes = maxTime % 60;
                    maxTimeStr = minutes == 0 ? $"{hours} saat" : $"{hours} saat {minutes} dakika";
                }

                var templateData = new Dictionary<string, object>
                {
                    ["customer"] = new 
                    { 
                        name = customer.Name,
                        address = customer.Address
                    },
                    ["stop"] = new 
                    { 
                        estimatedArrivalTime = $"{minTimeStr} - {maxTimeStr}"
                    },
                    ["driver"] = new 
                    { 
                        name = driver?.Name ?? "Teslimat Görevlisi",
                        phone = driver?.Phone ?? workspace.PhoneNumber
                    },
                    ["trackingUrl"] = "https://app.yolpilot.com/tracking",
                    ["workspace"] = new
                    {
                        name = workspace.Name,
                        email = workspace.Email,
                        phoneNumber = workspace.PhoneNumber
                    }
                };

                // Email gönder
                if (!string.IsNullOrEmpty(customer.Email))
                {
                    try
                    {
                        var (subject, body) = await _templateService.GetProcessedEmailContentAsync(
                            workspace.Id,
                            TemplateType.CheckIn,
                            templateData);

                        if (!string.IsNullOrEmpty(subject) && !string.IsNullOrEmpty(body))
                        {
                            await _emailService.SendNearbyNotificationToCustomerContactsAsync(
                                customer.Id,
                                customer.Name,
                                minTimeStr,
                                maxTimeStr);
                        }
                        else
                        {
                            await _emailService.SendNearbyNotificationToCustomerContactsAsync(
                                customer.Id,
                                customer.Name,
                                minTimeStr,
                                maxTimeStr);
                        }
                        
                        _logger.LogInformation($"Delivery approaching email sent to {customer.Email} with ETA: {minTimeStr} - {maxTimeStr}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send delivery approaching email");
                    }
                }

                // WhatsApp kontrolü ve gönderimi
                bool sendWhatsApp = workspace.WhatsAppMode != WhatsAppMode.Disabled && 
                                   workspace.WhatsAppNotifyCheckIn;
                
                if (sendWhatsApp)
                {
                    var canSendWhatsApp = await _subscriptionService.CanSendWhatsApp(User.WorkspaceId);
                    sendWhatsApp = canSendWhatsApp;
                }

                if (sendWhatsApp && customer.WhatsAppOptIn && !string.IsNullOrEmpty(customer.WhatsApp))
                {
                    try
                    {
                        var whatsAppSuccess = await _whatsAppService.SendApproachingMessage(
                            workspace,
                            customer.WhatsApp,
                            customer.Name,
                            minTimeStr,
                            maxTimeStr,
                            driver?.Name,
                            driver?.Phone);

                        if (whatsAppSuccess)
                        {
                            _logger.LogInformation($"Approaching WhatsApp sent to {customer.Name} ({customer.WhatsApp})");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to send WhatsApp to {customer.WhatsApp}");
                    }
                }
            }
        }

        // ✅ Türkiye saatini al (server UTC'de olabilir)
        var turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
        var turkeyNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, turkeyTimeZone);

        var updateCommand = new UpdateJourneyStopCommand
        {
            JourneyId = request.JourneyId,
            StopId = request.StopId,
            Status = JourneyStopStatus.InProgress,
            CheckInTime = turkeyNow,
            AuthenticatedUserId = request.AuthenticatedUserId
        };

        var success = await _mediator.Send(updateCommand, cancellationToken);

        // ✅ Gecikme bilgilerini al
        var stop = await _context.JourneyStops
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.StopId, cancellationToken);

        // ✅ Response oluştur
        return new Responses.Journeys.CheckInResponse
        {
            Success = success,
            RequiresDelayReason = stop != null && stop.NewDelay >= 15,
            NewDelay = stop?.NewDelay ?? 0,
            CumulativeDelay = stop?.CumulativeDelay ?? 0
        };
    }
}

public class CompleteJourneyStopCommand : BaseAuthenticatedCommand<bool>
{
    [JsonIgnore] public override bool RequiresDriver => true;
    [JsonIgnore] public override bool RequiresWorkspaceAccess => true;
    [JsonIgnore] public int JourneyId { get; set; }
    [JsonIgnore] public int StopId { get; set; }

    public string? Notes { get; set; }
    public string? ReceiverName { get; set; } // ✅ YENİ EKLENEN

    [JsonIgnore] public IFormFile? SignatureFile { get; set; }
    [JsonIgnore] public IFormFile? PhotoFile { get; set; }

    // YENİ: Çoklu fotoğraf desteği
    [JsonIgnore] public List<IFormFile>? PhotoFiles { get; set; }

    // ✅ YENİ: Depo dönüşü için özel alanlar
    public decimal? EndKilometer { get; set; }
    public string? FuelLevel { get; set; } // full, three_quarters, half, quarter, empty
    public string? VehicleCondition { get; set; } // good, needs_cleaning, needs_maintenance, damaged
}

public class CompleteJourneyStopCommandHandler : BaseAuthenticatedCommandHandler<CompleteJourneyStopCommand, bool>
{
    private readonly IMediator _mediator;
    private readonly AppDbContext _context;

    public CompleteJourneyStopCommandHandler(IUserService userService, IMediator mediator, AppDbContext context)
        : base(userService)
    {
        _mediator = mediator;
        _context = context;
    }

    protected override async Task<bool> HandleCommand(CompleteJourneyStopCommand request, CancellationToken cancellationToken)
    {
        // Önce stop'u kontrol et
        var stop = await _context.JourneyStops
            .Include(s => s.RouteStop)
                .ThenInclude(rs => rs.Customer)
            .Include(s => s.Journey)
                .ThenInclude(j => j.Vehicle)
            .FirstOrDefaultAsync(s =>
                s.Id == request.StopId &&
                s.JourneyId == request.JourneyId,
                cancellationToken);
        
        if (stop == null)
        {
            throw new ApiException("Stop not found", 404);
        }

        // ✅ Türkiye saatini al (server UTC'de olabilir)
        var turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
        var checkOutTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, turkeyTimeZone);
        var checkInTime = stop.CheckInTime ?? checkOutTime; // CheckInTime yoksa, CheckOutTime'ı kullan

        // ✅ Depo durağı kontrolü - RouteStop null ise VEYA son durak ise depo dönüşüdür
        var maxOrder = await _context.JourneyStops
            .Where(s => s.JourneyId == request.JourneyId)
            .MaxAsync(s => (int?)s.Order, cancellationToken) ?? 0;
        var isLastStop = maxOrder > 0 && stop.Order == maxOrder;
        var isDepotReturn = stop.RouteStop == null || isLastStop;

        // ✅ ReceiverName kontrolü - SADECE normal duraklar için zorunlu
        if (!isDepotReturn && string.IsNullOrWhiteSpace(request.ReceiverName))
        {
            throw new ApiException("Teslim alan kişinin adı zorunludur.", 400);
        }

        // YENİ: Çoklu fotoğraf için kontrol
        var hasPhoto = request.PhotoFile != null || (request.PhotoFiles != null && request.PhotoFiles.Any());

        // Proof/Signature/Photo validasyonları SADECE normal duraklar için (depo değil)
        if (!isDepotReturn)
        {
            // İmza zorunluluğu kontrolü
            if (stop.RouteStop.SignatureRequired && request.SignatureFile == null)
            {
                throw new ApiException("Bu teslimat için imza zorunludur.", 400);
            }

            // Fotoğraf zorunluluğu kontrolü - en az bir fotoğraf olmalı
            if (stop.RouteStop.PhotoRequired && !hasPhoto)
            {
                throw new ApiException("Bu teslimat için en az bir fotoğraf zorunludur.", 400);
            }

            // ProofOfDeliveryRequired kontrolü (ikisi birden gerekli)
            if (stop.RouteStop.ProofOfDeliveryRequired &&
                (request.SignatureFile == null || !hasPhoto))
            {
                throw new ApiException("Bu teslimat için hem imza hem fotoğraf zorunludur.", 400);
            }
        }

        // ✅ Depo dönüşü için kilometre ve araç durumu kontrolü
        if (isDepotReturn && !request.EndKilometer.HasValue)
        {
            throw new ApiException("Depo dönüşünde araç kilometresi zorunludur.", 400);
        }

        if (isDepotReturn && string.IsNullOrWhiteSpace(request.VehicleCondition))
        {
            throw new ApiException("Depo dönüşünde araç durumu zorunludur.", 400);
        }

        // ✅ Kilometre validasyonu: EndKm aracın mevcut kilometresinden veya sefer başlangıç kilometresinden küçük olamaz
        if (isDepotReturn && request.EndKilometer.HasValue)
        {
            // Journey'nin StartKm'sinden küçük olamaz
            if (stop.Journey.StartKm.HasValue && request.EndKilometer.Value < stop.Journey.StartKm.Value)
            {
                throw new ApiException(
                    $"Bitiş kilometresi ({request.EndKilometer.Value:N0} km) seferin başlangıç kilometresinden ({stop.Journey.StartKm.Value:N0} km) küçük olamaz.",
                    400);
            }

            // Vehicle'ın CurrentKm'sinden küçük olamaz
            if (stop.Journey.Vehicle?.CurrentKm.HasValue == true && request.EndKilometer.Value < stop.Journey.Vehicle.CurrentKm.Value)
            {
                throw new ApiException(
                    $"Bitiş kilometresi ({request.EndKilometer.Value:N0} km) aracın mevcut kilometresinden ({stop.Journey.Vehicle.CurrentKm.Value:N0} km) küçük olamaz.",
                    400);
            }
        }

        // Mevcut update command'i çağır
        var updateCommand = new UpdateJourneyStopCommand
        {
            JourneyId = request.JourneyId,
            StopId = request.StopId,
            Status = JourneyStopStatus.Completed,
            CheckInTime = checkInTime, // ✅ CheckInTime'ı da set et
            CheckOutTime = checkOutTime,
            Notes = request.Notes,
            ReceiverName = isDepotReturn ? "Depo Teslimi" : request.ReceiverName, // ✅ Depo dönüşü için otomatik
            SignatureFile = request.SignatureFile,
            PhotoFile = request.PhotoFile,
            PhotoFiles = request.PhotoFiles, // YENİ
            EndKilometer = request.EndKilometer, // ✅ Depo dönüşü için
            FuelLevel = request.FuelLevel, // ✅ Depo dönüşü için
            VehicleCondition = request.VehicleCondition, // ✅ Depo dönüşü için
            AuthenticatedUserId = request.AuthenticatedUserId
        };

        return await _mediator.Send(updateCommand, cancellationToken);
    }
}

public class FailJourneyStopCommand : BaseAuthenticatedCommand<bool>
{
    [JsonIgnore] public override bool RequiresDriver => true;
    [JsonIgnore] public override bool RequiresWorkspaceAccess => true;
    [JsonIgnore] public int JourneyId { get; set; }
    [JsonIgnore] public int StopId { get; set; }

    public string FailureReason { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class FailJourneyStopCommandValidator : AbstractValidator<FailJourneyStopCommand>
{
    public FailJourneyStopCommandValidator()
    {
        RuleFor(x => x.FailureReason).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public class FailJourneyStopCommandHandler : BaseAuthenticatedCommandHandler<FailJourneyStopCommand, bool>
{
    private readonly IMediator _mediator;
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IWhatsAppService _whatsAppService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ITemplateService _templateService;
    private readonly ILogger<FailJourneyStopCommandHandler> _logger;
    private readonly IConfiguration _configuration;

    public FailJourneyStopCommandHandler(
        IUserService userService, 
        IMediator mediator,
        AppDbContext context,
        IEmailService emailService,
        IWhatsAppService whatsAppService,
        ISubscriptionService subscriptionService,
        ITemplateService templateService,
        ILogger<FailJourneyStopCommandHandler> logger,
        IConfiguration configuration)
        : base(userService)
    {
        _mediator = mediator;
        _context = context;
        _emailService = emailService;
        _whatsAppService = whatsAppService;
        _subscriptionService = subscriptionService;
        _templateService = templateService;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task<bool> HandleCommand(FailJourneyStopCommand request, CancellationToken cancellationToken)
    {
        var stop = await _context.JourneyStops
            .Include(s => s.RouteStop)
                .ThenInclude(rs => rs.Customer)
            .Include(s => s.Journey)
                .ThenInclude(j => j.Driver)
            .FirstOrDefaultAsync(s => 
                s.Id == request.StopId && 
                s.JourneyId == request.JourneyId,
                cancellationToken);

        // ✅ Türkiye saatini al (server UTC'de olabilir)
        var turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
        var turkeyNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, turkeyTimeZone);

        var updateCommand = new UpdateJourneyStopCommand
        {
            JourneyId = request.JourneyId,
            StopId = request.StopId,
            Status = JourneyStopStatus.Failed,
            CheckOutTime = turkeyNow,
            FailureReason = request.FailureReason,
            Notes = request.Notes,
            AuthenticatedUserId = request.AuthenticatedUserId
        };

        var result = await _mediator.Send(updateCommand, cancellationToken);

        if (stop?.RouteStop?.Customer != null)
        {
            var customer = stop.RouteStop.Customer;
            var driver = stop.Journey?.Driver;
            var trackingToken = GenerateTrackingToken(request.JourneyId, request.StopId);
            var baseUrl = _configuration["Tracking:ApiBaseUrl"] ?? "https://api.yolpilot.com";
            var trackingUrl = $"{baseUrl}/tracking/delivery/{request.JourneyId}/{request.StopId}?token={trackingToken}";
            var rescheduleUrl = $"{trackingUrl}&reschedule=true";

            var workspace = await _context.Workspaces
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == User.WorkspaceId, cancellationToken);

            var templateData = new Dictionary<string, object>
            {
                ["customer"] = new 
                { 
                    name = customer.Name,
                    address = customer.Address
                },
                ["failureReason"] = TranslateFailureReason(request.FailureReason),
                ["failureTime"] = DateTime.Now.ToString("HH:mm"),
                ["driver"] = new 
                { 
                    name = driver?.Name ?? "Teslimat Görevlisi",
                    phone = driver?.Phone ?? workspace.PhoneNumber
                },
                ["rescheduleUrl"] = rescheduleUrl,
                ["workspace"] = new
                {
                    name = workspace.Name,
                    email = workspace.Email,
                    phoneNumber = workspace.PhoneNumber
                }
            };

            // Email gönder
            if (!string.IsNullOrEmpty(customer.Email))
            {
                try
                {
                    var (subject, body) = await _templateService.GetProcessedEmailContentAsync(
                        workspace.Id,
                        TemplateType.DeliveryFailed,
                        templateData);

                    if (!string.IsNullOrEmpty(subject) && !string.IsNullOrEmpty(body))
                    {
                        await _emailService.SendDeliveryFailedToCustomerContactsAsync(
                            customer.Id,
                            customer.Name,
                            request.FailureReason,
                            request.Notes ?? "",
                            trackingUrl);
                    }
                    else
                    {
                        await _emailService.SendDeliveryFailedToCustomerContactsAsync(
                            customer.Id,
                            customer.Name,
                            request.FailureReason,
                            request.Notes ?? "",
                            trackingUrl);
                    }
                    
                    _logger.LogInformation($"Delivery failed email sent to {customer.Email}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send delivery failed email");
                }
            }

            // WhatsApp kontrolü
            bool sendWhatsApp = workspace.WhatsAppMode != WhatsAppMode.Disabled && 
                               workspace.WhatsAppNotifyFailure;
            
            if (sendWhatsApp)
            {
                var canSendWhatsApp = await _subscriptionService.CanSendWhatsApp(User.WorkspaceId);
                sendWhatsApp = canSendWhatsApp;
            }

            if (sendWhatsApp && customer.WhatsAppOptIn && !string.IsNullOrEmpty(customer.WhatsApp))
            {
                try
                {
                    var success = await _whatsAppService.SendDeliveryFailedMessage(
                        workspace,
                        customer.WhatsApp,
                        customer.Name,
                        request.FailureReason,
                        trackingUrl,
                        null);

                    if (success)
                    {
                        _logger.LogInformation($"Delivery failed WhatsApp sent to {customer.Name} ({customer.WhatsApp})");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send WhatsApp to {customer.WhatsApp}");
                }
            }
        }

        return result;
    }

    private string GenerateTrackingToken(int journeyId, int stopId)
    {
        var secret = _configuration["Tracking:Secret"] ?? "YolPilot2024Secret!";
        var workspaceId = User.WorkspaceId;
        var input = $"{journeyId}-{stopId}-{workspaceId}-{secret}";
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hash).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private string TranslateFailureReason(string failureReason)
    {
        return failureReason switch
        {
            "Customer not available" => "Müşteri adreste bulunamadı",
            "Wrong address" => "Adres bilgisi hatalı",
            "Customer refused" => "Müşteri teslimatı kabul etmedi",
            "Package damaged" => "Paket hasarlı",
            "Vehicle breakdown" => "Araç arızası",
            "Weather conditions" => "Hava koşulları nedeniyle",
            "Traffic accident" => "Trafik kazası",
            "Access restricted" => "Adrese erişim kısıtlı",
            "Other" => "Diğer nedenler",
            _ => failureReason
        };
    }
}
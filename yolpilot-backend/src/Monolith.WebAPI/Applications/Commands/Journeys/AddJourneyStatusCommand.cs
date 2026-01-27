using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Journeys;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Responses.Journeys;
using Monolith.WebAPI.Services.Members;
using Monolith.WebAPI.Services.Notifications;

namespace Monolith.WebAPI.Applications.Commands.Journeys;

public class AddJourneyStatusCommand : BaseAuthenticatedCommand<JourneyStatusResponse>
{
    [JsonIgnore] public override bool RequiresDriver => true; // Driver ve üstü yapabilir
    [JsonIgnore] public override bool RequiresWorkspaceAccess => true;
    [JsonIgnore] public int JourneyId { get; set; }
    
    public int StopId { get; set; }
    public JourneyStatusType Status { get; set; }
    public string Notes { get; set; }
    
    // ✅ YENİ EKLENEN FIELD'LAR
    public string FailureReason { get; set; }
    public string SignatureBase64 { get; set; }
    public string PhotoBase64 { get; set; }
    public string ReceiverName { get; set; } // YENİ ALAN EKLEYİN

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public Dictionary<string, string> AdditionalValues { get; set; }

    public JourneyStatus ToEntity() => new(this);
}

public class AddStatusCommandValidator : AbstractValidator<AddJourneyStatusCommand>
{
    public AddStatusCommandValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Notes).MaximumLength(1024);
        
        // ✅ YENİ VALIDATION KURALLARI
        RuleFor(x => x.FailureReason).MaximumLength(500);
        
        // ✅ DÜZELTİLMİŞ Base64 validation - data URL prefix'ini de kabul eder
        RuleFor(x => x.SignatureBase64).Must(BeValidBase64).When(x => !string.IsNullOrEmpty(x.SignatureBase64))
            .WithMessage("Invalid signature format");
        RuleFor(x => x.PhotoBase64).Must(BeValidBase64).When(x => !string.IsNullOrEmpty(x.PhotoBase64))
            .WithMessage("Invalid photo format");
        
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
    }
    
    // ✅ DÜZELTİLMİŞ - Data URL prefix'li base64'ü de kabul eder
    private bool BeValidBase64(string base64String)
    {
        if (string.IsNullOrEmpty(base64String))
            return true;
        
        try
        {
            var base64Data = base64String;
            
            // Data URL formatında gelebilir: "data:image/png;base64,..." veya "data:image/jpeg;base64,..."
            if (base64String.Contains(","))
            {
                var parts = base64String.Split(',');
                if (parts.Length == 2 && parts[0].StartsWith("data:image/"))
                {
                    // Sadece base64 kısmını al
                    base64Data = parts[1];
                }
            }
            
            // Base64 string'i decode etmeyi dene
            var bytes = Convert.FromBase64String(base64Data);
            
            // Opsiyonel: Dosya boyutu kontrolü (örn: max 10MB)
            if (bytes.Length > 10 * 1024 * 1024)
            {
                return false; // 10MB'dan büyük
            }
            
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public class AddStatusCommandHandler(AppDbContext context, IUserService userService, INotificationService notificationService)
    : BaseAuthenticatedCommandHandler<AddJourneyStatusCommand, JourneyStatusResponse>(userService)
{
    override protected async Task<JourneyStatusResponse> HandleCommand(AddJourneyStatusCommand request,
        CancellationToken cancellationToken)
    {
        // Journey'yi workspace kontrolü ile al - Tracking açık
        var journey = await context.Journeys
            .Include(x => x.Route)
            .Include(x => x.Stops)
            .FirstOrDefaultAsync(x => x.Id == request.JourneyId && 
                                     x.Route.WorkspaceId == User.WorkspaceId, 
                                     cancellationToken);

        if (journey == null)
            throw new ApiException("Journey not found.", 404);

        // Driver'lar sadece kendi journey'lerine status ekleyebilir
        if (User.IsDriver && !User.IsDispatcher && !User.IsAdmin && !User.IsSuperAdmin)
        {
            var driver = await context.Set<Data.Workspace.Driver>()
                .FirstOrDefaultAsync(d => d.WorkspaceId == User.WorkspaceId && 
                                         d.Email == User.Email,
                                         cancellationToken);
            
            if (driver == null || journey.DriverId != driver.Id)
                throw new ApiException("You can only update status for your own journeys.", 403);
        }

        // JourneyStop'u al ve status'ünü güncelle
        var journeyStop = await context.JourneyStops
            .FirstOrDefaultAsync(s => s.Id == request.StopId && s.JourneyId == request.JourneyId, 
                     cancellationToken);
        
        if (journeyStop == null)
            throw new ApiException("Stop not found in this journey.", 404);

        // ✅ JourneyStop status'ünü güncelle
        switch (request.Status)
        {
            case JourneyStatusType.Arrived:
                // Check-in yapıldı, stop artık "InProgress" durumunda
                journeyStop.UpdateStatus(JourneyStopStatus.InProgress);
                break;
                
            case JourneyStatusType.Completed:
                // Stop tamamlandı
                journeyStop.UpdateStatus(JourneyStopStatus.Completed);
                break;
                
            case JourneyStatusType.Cancelled:
                // ✅ Stop başarısız oldu - FailureReason kontrolü
                if (string.IsNullOrEmpty(request.FailureReason) && string.IsNullOrEmpty(request.Notes))
                    throw new ApiException("Başarısızlık nedeni belirtilmelidir.", 400);
                    
                journeyStop.UpdateStatus(JourneyStopStatus.Failed);
                break;
                
            case JourneyStatusType.Delayed:
                // Stop gecikti ama hala devam ediyor
                journeyStop.UpdateStatus(JourneyStopStatus.InProgress);
                break;
                
            case JourneyStatusType.Processing:
                // İşlem yapılıyor (yükleme/boşaltma vb.)
                journeyStop.UpdateStatus(JourneyStopStatus.InProgress);
                break;
                
            case JourneyStatusType.OnHold:
                // Stop bekletiliyor
                journeyStop.UpdateStatus(JourneyStopStatus.InProgress);
                break;
                
            case JourneyStatusType.InTransit:
                // Yolda - Status değişikliği yapmaya gerek yok
                break;
        }

        // ✅ Base64 string'lerden data URL prefix'ini temizle (eğer varsa)
        if (!string.IsNullOrEmpty(request.SignatureBase64))
        {
            request.SignatureBase64 = CleanBase64String(request.SignatureBase64);
            
            // TODO: İleride blob storage'a kaydedilip sadece URL saklanabilir
            // var signatureUrl = await SaveToBlob(request.SignatureBase64, "signatures");
            // request.SignatureBase64 = null; // Base64'ü silip URL'yi başka bir field'da saklayabiliriz
        }
        
        if (!string.IsNullOrEmpty(request.PhotoBase64))
        {
            request.PhotoBase64 = CleanBase64String(request.PhotoBase64);
            
            // TODO: İleride blob storage'a kaydedilip sadece URL saklanabilir
            // var photoUrl = await SaveToBlob(request.PhotoBase64, "photos");
            // request.PhotoBase64 = null; // Base64'ü silip URL'yi başka bir field'da saklayabiliriz
        }

        // JourneyStatus kaydını oluştur (log/history için)
        var status = request.ToEntity();
        await context.JourneyStatuses.AddAsync(status, cancellationToken);
        
        // Değişiklikleri kaydet
        await context.SaveChangesAsync(cancellationToken);

        // Status değişikliği notification'ı gönder
        try
        {
            var statusName = request.Status switch
            {
                JourneyStatusType.InTransit => "Yolda",
                JourneyStatusType.Arrived => "Varış",
                JourneyStatusType.Processing => "İşlem Yapılıyor",
                JourneyStatusType.Completed => "Tamamlandı",
                JourneyStatusType.Delayed => "Gecikme",
                JourneyStatusType.Cancelled => "İptal",
                JourneyStatusType.OnHold => "Beklemede",
                _ => request.Status.ToString()
            };

            // Driver kendisi status ekliyorsa, dispatcher/admin'lere bildir
            // Dispatcher/Admin status ekliyorsa, driver'a bildir
            var targetUserId = string.Empty;
            if (User.IsDriver && !User.IsDispatcher && !User.IsAdmin)
            {
                // Driver status ekledi - workspace admin/dispatcher'lara bildir (şimdilik skip edelim, çok spam olur)
            }
            else
            {
                // Admin/Dispatcher status ekledi - driver'a bildir
                if (journey.Route?.Driver?.User != null)
                {
                    await notificationService.CreateJourneyStatusChangedNotificationAsync(
                        journey.Route.WorkspaceId,
                        journey.Route.Driver.User.Id,
                        journey.Id,
                        journey.Route.Name,
                        statusName
                    );
                }
            }
        }
        catch (Exception ex)
        {
            // Notification hatası journey status'u etkilemez - sadece log
            Console.WriteLine($"Error sending status change notification: {ex.Message}");
        }

        return new JourneyStatusResponse(status);
    }
    
    // ✅ Helper method: Data URL prefix'ini temizle
    private string CleanBase64String(string base64String)
    {
        if (string.IsNullOrEmpty(base64String))
            return base64String;
        
        // "data:image/png;base64," veya "data:image/jpeg;base64," gibi prefix'leri kaldır
        if (base64String.Contains(","))
        {
            var parts = base64String.Split(',');
            if (parts.Length == 2 && parts[0].StartsWith("data:image/"))
            {
                return parts[1]; // Sadece base64 kısmını döndür
            }
        }
        
        return base64String; // Prefix yoksa olduğu gibi döndür
    }
}
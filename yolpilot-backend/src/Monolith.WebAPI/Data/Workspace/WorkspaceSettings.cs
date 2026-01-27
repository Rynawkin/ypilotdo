// src/Monolith.WebAPI/Data/Workspace/WorkspaceSettings.cs

#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monolith.WebAPI.Data.Workspace;

/// <summary>
/// Workspace için JSON olarak saklanacak ek ayarlar
/// </summary>
public class WorkspaceSettings
{
    // Company Settings
    public string? Logo { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? TaxNumber { get; set; }
    public string? Website { get; set; }
    
    // Regional Settings
    public string DateFormat { get; set; } = "DD/MM/YYYY";
    public string FirstDayOfWeek { get; set; } = "monday";
    
    // Delivery Settings
    public int MaxDeliveriesPerRoute { get; set; } = 50;
    public bool DefaultSignatureRequired { get; set; } = false;
    public bool DefaultPhotoRequired { get; set; } = false;

    // Working Hours - JSON olarak saklanacak
    private string? _workingHoursJson;
    
    [NotMapped]
    public Dictionary<string, WorkingHours>? WorkingHours
    {
        get => string.IsNullOrEmpty(_workingHoursJson)
            ? new Dictionary<string, WorkingHours>
            {
                ["monday"] = new WorkingHours { Start = "09:00", End = "18:00", IsActive = true },
                ["tuesday"] = new WorkingHours { Start = "09:00", End = "18:00", IsActive = true },
                ["wednesday"] = new WorkingHours { Start = "09:00", End = "18:00", IsActive = true },
                ["thursday"] = new WorkingHours { Start = "09:00", End = "18:00", IsActive = true },
                ["friday"] = new WorkingHours { Start = "09:00", End = "18:00", IsActive = true },
                ["saturday"] = new WorkingHours { Start = "09:00", End = "14:00", IsActive = false },
                ["sunday"] = new WorkingHours { Start = "09:00", End = "14:00", IsActive = false }
            }
            : JsonSerializer.Deserialize<Dictionary<string, WorkingHours>>(_workingHoursJson);
        set => _workingHoursJson = value == null
            ? null
            : JsonSerializer.Serialize(value);
    }
    
    [JsonIgnore]
    [Column("WorkingHoursJson")]
    public string? WorkingHoursJson
    {
        get => _workingHoursJson;
        set => _workingHoursJson = value;
    }
    
    public PrioritySettings? PrioritySettings { get; set; }
    public bool AutoOptimize { get; set; } = true;
    public bool TrafficConsideration { get; set; } = true;
    
    // Notification Settings
    public bool EmailNotifications { get; set; } = true;
    public bool SmsNotifications { get; set; } = false;
    public string? NotificationEmail { get; set; }
    public string? NotificationPhone { get; set; }
    public NotificationEvents? NotificationEvents { get; set; }
    
    // WhatsApp Settings
    public WhatsAppSettings? WhatsAppSettings { get; set; }

    // Email Templates
    public EmailTemplates? EmailTemplates { get; set; }

    // Delay Alert Settings
    public DelayAlertSettings? DelayAlertSettings { get; set; }
}

/// <summary>
/// WhatsApp bildirim ayarları
/// </summary>
public class WhatsAppSettings
{
    public bool Enabled { get; set; } = false;
    public bool EnableWhatsAppForJourneyStart { get; set; } = false; // Sefer başladığında
    public bool EnableWhatsAppForCheckIn { get; set; } = false; // Önceki durağa varıldığında
    public bool EnableWhatsAppForCompletion { get; set; } = false; // Teslimat tamamlandığında
    public bool EnableWhatsAppForFailure { get; set; } = false; // Teslimat başarısız olduğunda
    
    // Template ID'leri (Twilio veya Meta Business API için)
    public string? JourneyStartTemplateId { get; set; }
    public string? ApproachingTemplateId { get; set; }
    public string? CompletedTemplateId { get; set; }
    public string? FailedTemplateId { get; set; }
    
    // WhatsApp Business bilgileri
    public string? BusinessPhoneNumber { get; set; } // İşletme WhatsApp numarası
    public string? BusinessDisplayName { get; set; } // Görünen isim
}

/// <summary>
/// Email template özelleştirmeleri
/// </summary>
public class EmailTemplates
{
    // Başlıklar
    public string? CompanyName { get; set; } = "YolPilot";
    public string? SupportEmail { get; set; } = "destek@yolpilot.com";
    public string? SupportPhone { get; set; } = "0850 756 62 67";
    
    // Teslimat Başarılı Email
    public DeliveryCompletedTemplate? DeliveryCompleted { get; set; }
    
    // Teslimat Başarısız Email
    public DeliveryFailedTemplate? DeliveryFailed { get; set; }
    
    // Teslimat Yaklaşıyor Email
    public DeliveryApproachingTemplate? DeliveryApproaching { get; set; }
    
    // Sefer Başladı Email
    public JourneyStartedTemplate? JourneyStarted { get; set; }
}

public class DeliveryCompletedTemplate
{
    public string? HeaderText { get; set; } = "Teslimatınız Tamamlandı!";
    public string? SuccessMessage { get; set; } = "Siparişiniz başarıyla teslim edildi.";
    public string? ThankYouMessage { get; set; } = "Bizi tercih ettiğiniz için teşekkür ederiz.";
    public string? FeedbackPrompt { get; set; } = "Deneyiminizi paylaşın! Görüşleriniz bizim için çok değerli.";
    public bool ShowProofOfDelivery { get; set; } = true;
    public bool ShowFeedbackButton { get; set; } = true;
    public bool ShowTrackingLink { get; set; } = true;
}

public class DeliveryFailedTemplate
{
    public string? HeaderText { get; set; } = "Teslimatınız Gerçekleştirilemedi";
    public string? ApologyMessage { get; set; } = "Üzülerek bildiririz ki bugünkü teslimatınızı gerçekleştiremedik.";
    public string? NextStepsMessage { get; set; } = "En geç 1 iş günü içinde sizinle iletişime geçeceğiz ve yeni teslimat zamanını birlikte belirleyeceğiz.";
    public string? ContactPrompt { get; set; } = "Teslimatınızı hızlandırmak için bizi arayabilirsiniz:";
    public bool ShowFailureReason { get; set; } = true;
    public bool ShowContactInfo { get; set; } = true;
}

public class DeliveryApproachingTemplate
{
    public string? HeaderText { get; set; } = "Teslimatınız Yaklaşıyor!";
    public string? ApproachingMessage { get; set; } = "Sürücümüz 30 dakika içinde sizinle olacak.";
    public string? PreparationMessage { get; set; } = "Lütfen teslimatı teslim alacak kişinin hazır bulunmasını sağlayın.";
    public bool ShowChecklist { get; set; } = true;
    public bool ShowEstimatedTime { get; set; } = true;
}

public class JourneyStartedTemplate
{
    public string? HeaderText { get; set; } = "Siparişiniz Yolda!";
    public string? StartedMessage { get; set; } = "Siparişiniz teslimat için yola çıktı. Sürücümüz en kısa sürede adresinizde olacak.";
    public string? TrackingMessage { get; set; } = "Teslimatınız yaklaştığında size tekrar bilgilendirme yapacağız.";
    public bool ShowDriverName { get; set; } = true;
    public bool ShowEstimatedTime { get; set; } = true;
}

/// <summary>
/// Çalışma saatleri için model
/// </summary>
public class WorkingHours
{
    public string Start { get; set; } = "09:00";
    public string End { get; set; } = "18:00";
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Öncelik ayarları için model
/// </summary>
public class PrioritySettings
{
    public bool EnablePriority { get; set; } = true;
    public int HighPriorityWeight { get; set; } = 3;
    public int MediumPriorityWeight { get; set; } = 2;
    public int LowPriorityWeight { get; set; } = 1;
    public bool ConsiderTimeWindows { get; set; } = true;
}

/// <summary>
/// Bildirim olayları için model
/// </summary>
public class NotificationEvents
{
    public bool OnRouteCreated { get; set; } = true;
    public bool OnRouteAssigned { get; set; } = true;
    public bool OnRouteStarted { get; set; } = true;
    public bool OnRouteCompleted { get; set; } = true;
    public bool OnDeliveryFailed { get; set; } = true;
    public bool OnDeliveryCompleted { get; set; } = false;
    public bool OnDriverCheckIn { get; set; } = false;
    public bool OnDriverDelayed { get; set; } = true;
}

/// <summary>
/// Gecikme uyarı ayarları
/// </summary>
public class DelayAlertSettings
{
    /// <summary>
    /// Gecikme uyarılarını aktif et/devre dışı bırak
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Kaç saat gecikme sonrası uyarı gönderilsin (örn: 1, 2, 3)
    /// </summary>
    public int ThresholdHours { get; set; } = 1;

    /// <summary>
    /// Gecikme raporlarının gönderileceği email adresleri (virgülle ayrılmış)
    /// Örnek: "admin@company.com,manager@company.com"
    /// </summary>
    public string? AlertEmails { get; set; }

    /// <summary>
    /// Email adreslerini liste olarak döndür
    /// </summary>
    public List<string> GetEmailList()
    {
        if (string.IsNullOrWhiteSpace(AlertEmails))
            return new List<string>();

        return AlertEmails
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(e => e.Trim())
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .ToList();
    }
}

#nullable restore

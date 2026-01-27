// src/Monolith.WebAPI/Responses/Settings/SettingsResponses.cs

using System.Text.Json.Serialization;

namespace Monolith.WebAPI.Responses.Settings;

public class WorkspaceSettingsResponse
{
    public string Name { get; set; }
    public string Logo { get; set; }
    public string Address { get; set; }
    public string City { get; set; }
    public string PostalCode { get; set; }
    public string TaxNumber { get; set; }
    public string PhoneNumber { get; set; }
    public string Email { get; set; }
    public string Website { get; set; }
    public string Currency { get; set; }
    public string TimeZone { get; set; }
    public string Language { get; set; }
    public string DateFormat { get; set; }
    public string FirstDayOfWeek { get; set; }
}

public class DeliverySettingsResponse
{
    public int DefaultServiceTime { get; set; }
    public int MaxDeliveriesPerRoute { get; set; }
    public bool DefaultSignatureRequired { get; set; }  // YENİ
    public bool DefaultPhotoRequired { get; set; }      // YENİ
    public Dictionary<string, WorkingHours> WorkingHours { get; set; }
    public PrioritySettings PrioritySettings { get; set; }
    public bool AutoOptimize { get; set; }
    public bool TrafficConsideration { get; set; }
    public double? CostPerKm { get; set; }
    public double? CostPerHour { get; set; }
}

public class WorkingHours
{
    public string Start { get; set; }
    public string End { get; set; }
    public bool Enabled { get; set; }
}


public class WhatsAppSettingsResponse
{
    public string Mode { get; set; } = "disabled"; // disabled, shared, custom
    public bool Enabled { get; set; }
    public bool EnableWhatsAppForJourneyStart { get; set; }
    public bool EnableWhatsAppForCheckIn { get; set; }
    public bool EnableWhatsAppForCompletion { get; set; }
    public bool EnableWhatsAppForFailure { get; set; }
}

public class PrioritySettings
{
    public PriorityLevel High { get; set; }
    public PriorityLevel Normal { get; set; }
    public PriorityLevel Low { get; set; }
}

public class PriorityLevel
{
    public string Color { get; set; }
    public int MaxDelay { get; set; }
}

public class NotificationSettingsResponse
{
    public bool EmailNotifications { get; set; }
    public bool SmsNotifications { get; set; }
    public string NotificationEmail { get; set; }
    public string NotificationPhone { get; set; }
    public WhatsAppSettingsResponse WhatsAppSettings { get; set; }
    public NotificationEvents Events { get; set; }
}

public class NotificationEvents
{
    public bool RouteCompleted { get; set; }
    public bool DeliveryFailed { get; set; }
    public bool DriverDelayed { get; set; }
    public bool NewCustomer { get; set; }
    public bool DailyReport { get; set; }
}

// ✅ ThemeSettingsResponse KALDIRILDI

public class AllSettingsResponse
{
    public WorkspaceSettingsResponse Workspace { get; set; }
    public DeliverySettingsResponse Delivery { get; set; }
    public NotificationSettingsResponse Notifications { get; set; }
    // ✅ Theme property KALDIRILDI
}
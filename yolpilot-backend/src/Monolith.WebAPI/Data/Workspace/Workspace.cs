// src/Monolith.WebAPI/Data/Workspace/Workspace.cs

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Monolith.WebAPI.Applications.Commands.Workspace;
using Monolith.WebAPI.Data.Members;
using Monolith.WebAPI.Data.Workspace.Events;
using Route = Monolith.WebAPI.Data.Journeys.Route;
using Monolith.WebAPI.Data.Workspace.Enums;

namespace Monolith.WebAPI.Data.Workspace;

public class Workspace : BaseEntity
{
    // do not remove this constructor
    public Workspace()
    {
    }

    public Workspace(CreateWorkspaceCommand command)
    {
        Name = command.WorkspaceName;
        PhoneNumber = null;
        Email = null;
        DistanceUnit = "km";
        Currency = "TRY";
        TimeZone = "Europe/Istanbul";
        Language = "TR";
        CostPerKm = null;
        CostPerHour = null;
        DefaultServiceTime = TimeSpan.FromMinutes(15);
        MaximumDriverCount = 10;
        Active = true;
        
        // YENİ: Plan defaults - Start with Trial
        PlanType = PlanType.Trial;
        PlanStartDate = DateTime.UtcNow;
        TrialStartDate = DateTime.UtcNow;
        TrialEndDate = DateTime.UtcNow.AddDays(14);
        IsTrialUsed = false;
        CurrentMonthStops = 0;
        LastStopResetDate = DateTime.UtcNow;
        CurrentMonthAdditionalCharges = 0;
        CurrentMonthWhatsAppMessages = 0;
        LastWhatsAppResetDate = DateTime.UtcNow;
        AllowOverageCharges = true;
        
        // HYBRID WHATSAPP: Default olarak Disabled
        WhatsAppMode = WhatsAppMode.Disabled;

        // YENİ: Settings defaults
        Settings = new WorkspaceSettings
        {
            DateFormat = "DD/MM/YYYY",
            FirstDayOfWeek = "monday",
            EmailNotifications = true,
            SmsNotifications = false,
            WhatsAppSettings = new WhatsAppSettings
            {
                Enabled = false,
                EnableWhatsAppForJourneyStart = false,
                EnableWhatsAppForCheckIn = false,
                EnableWhatsAppForCompletion = false,
                EnableWhatsAppForFailure = false
            }
        };

        AddDomainEvent(new WorkspaceCreatedEvent(Id, Name));
    }

    public void Update(UpdateWorkspaceCommand command)
    {
        Name = command.Name;
        PhoneNumber = command.PhoneNumber;
        Email = command.Email;
        DistanceUnit = command.DistanceUnit;
        Currency = command.Currency;
        Language = command.Language;
        TimeZone = command.TimeZone;
        CostPerKm = command.CostPerKm;
        CostPerHour = command.CostPerHour;
        DefaultServiceTime = command.DefaultServiceTime;

        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new WorkspaceCreatedEvent(Id, Name));
    }

    // Settings Update Methods
    public void UpdateBasicSettings(string name, string phoneNumber, string email, string currency, string timeZone, string language)
    {
        if (!string.IsNullOrEmpty(name))
            Name = name;
        if (!string.IsNullOrEmpty(phoneNumber))
            PhoneNumber = phoneNumber;
        if (!string.IsNullOrEmpty(email))
            Email = email;
        if (!string.IsNullOrEmpty(currency))
            Currency = currency;
        if (!string.IsNullOrEmpty(timeZone))
            TimeZone = timeZone;
        if (!string.IsNullOrEmpty(language))
            Language = language;
            
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDeliverySettings(TimeSpan defaultServiceTime, double? costPerKm, double? costPerHour)
    {
        DefaultServiceTime = defaultServiceTime;
        CostPerKm = costPerKm;
        CostPerHour = costPerHour;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateNotificationSettings(string notificationEmail, string notificationPhone)
    {
        if (!string.IsNullOrEmpty(notificationEmail))
            Email = notificationEmail;
        if (!string.IsNullOrEmpty(notificationPhone))
            PhoneNumber = notificationPhone;
        UpdatedAt = DateTime.UtcNow;
    }
    
    // YENİ: WhatsApp Mode Methods
    public void SetWhatsAppMode(WhatsAppMode mode)
    {
        WhatsAppMode = mode;
        
        // Shared mode'a geçildiğinde custom bilgileri temizlenebilir (opsiyonel)
        if (mode == WhatsAppMode.Shared)
        {
            // Custom credentials'ı temizleme (opsiyonel, saklayabilirsiniz de)
            // DisconnectTwilio();
        }
        
        UpdatedAt = DateTime.UtcNow;
    }
    
    public bool CanSendWhatsApp()
    {
        if (WhatsAppMode == WhatsAppMode.Disabled)
            return false;
            
        if (WhatsAppMode == WhatsAppMode.Shared)
            return true; // YolPilot numarası her zaman hazır
            
        if (WhatsAppMode == WhatsAppMode.Custom)
            return HasTwilioIntegration(); // Custom için Twilio bağlı olmalı
            
        return false;
    }
    
    // YENİ: Twilio/WhatsApp Integration Methods
    public void ConnectTwilio(string accountSid, string authToken, string whatsAppNumber, bool useSandbox = false)
    {
        TwilioAccountSid = accountSid;
        TwilioAuthToken = authToken; // Production'da encrypt edilmeli
        TwilioWhatsAppNumber = whatsAppNumber;
        TwilioUseSandbox = useSandbox;
        TwilioConnectedAt = DateTime.UtcNow;
        TwilioVerified = true;
        
        // Custom mode'a otomatik geç
        if (WhatsAppMode != WhatsAppMode.Custom)
        {
            WhatsAppMode = WhatsAppMode.Custom;
        }
        
        UpdatedAt = DateTime.UtcNow;
    }

    public void DisconnectTwilio()
    {
        TwilioAccountSid = null;
        TwilioAuthToken = null;
        TwilioWhatsAppNumber = null;
        TwilioUseSandbox = false;
        TwilioConnectedAt = null;
        TwilioVerified = false;
        
        // Shared mode'a dön veya disable et
        if (WhatsAppMode == WhatsAppMode.Custom)
        {
            WhatsAppMode = WhatsAppMode.Disabled;
        }
        
        UpdatedAt = DateTime.UtcNow;
    }

    public bool HasTwilioIntegration()
    {
        return !string.IsNullOrEmpty(TwilioAccountSid) && 
               !string.IsNullOrEmpty(TwilioAuthToken) && 
               !string.IsNullOrEmpty(TwilioWhatsAppNumber) &&
               TwilioVerified;
    }
    
    // YENİ: Plan Update Method
    public void UpdatePlan(PlanType newPlan)
    {
        PlanType = newPlan;
        PlanStartDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    // YENİ: Usage Update Methods
    public void IncrementStopUsage(int count = 1)
    {
        CurrentMonthStops += count;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void IncrementWhatsAppUsage(int count = 1)
    {
        CurrentMonthWhatsAppMessages += count;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void AddAdditionalCharge(decimal amount)
    {
        CurrentMonthAdditionalCharges += amount;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void ResetMonthlyUsage()
    {
        CurrentMonthStops = 0;
        CurrentMonthAdditionalCharges = 0;
        CurrentMonthWhatsAppMessages = 0;
        LastStopResetDate = DateTime.UtcNow;
        LastWhatsAppResetDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    // Basic Properties
    [MaxLength(128)] public string Name { get; set; }
    [MaxLength(16)] public string PhoneNumber { get; set; }
    [MaxLength(64)] public string Email { get; set; }
    [MaxLength(10)] public string DistanceUnit { get; private set; }
    [MaxLength(10)] public string Currency { get; set; }
    [MaxLength(30)] public string TimeZone { get; set; }
    [MaxLength(30)] public string Language { get; set; }

    // Company Settings - YENİ EKLENEN
    [MaxLength(256)] public string Address { get; set; }
    [MaxLength(128)] public string City { get; set; }
    [MaxLength(20)] public string PostalCode { get; set; }
    [MaxLength(50)] public string TaxNumber { get; set; }
    [MaxLength(256)] public string Website { get; set; }

    // Regional Settings
    [MaxLength(20)] public string DateFormat { get; set; }
    [MaxLength(10)] public string FirstDayOfWeek { get; set; }


    // Delivery Settings
    public double? CostPerKm { get; set; }
    public double? CostPerHour { get; set; }
    public TimeSpan DefaultServiceTime { get; set; }
    public int MaximumDriverCount { get; private set; }
    public bool Active { get; private set; }
    
    // JSON Columns for Complex Settings - YENİ EKLENEN
    [Column(TypeName = "nvarchar(max)")]
    public string WorkingHours { get; set; }
    
    [Column(TypeName = "nvarchar(max)")]
    public string PrioritySettings { get; set; }
    
    // YENİ: Plan ve Kullanım Alanları
    public PlanType PlanType { get; set; }
    public DateTime? PlanStartDate { get; private set; }
    public DateTime? PlanEndDate { get; private set; }
    
    // Kullanım Takibi
    public int CurrentMonthStops { get; private set; }
    public DateTime LastStopResetDate { get; private set; }
    public decimal CurrentMonthAdditionalCharges { get; private set; }
    
    // WhatsApp Kullanımı
    public int CurrentMonthWhatsAppMessages { get; private set; }
    public DateTime LastWhatsAppResetDate { get; private set; }
    public bool AllowOverageCharges { get; set; } = true;
    
    // Trial Fields
    public DateTime? TrialStartDate { get; set; }
    public DateTime? TrialEndDate { get; set; }
    public bool IsTrialUsed { get; set; }

    // HYBRID WHATSAPP MODE
    public WhatsAppMode WhatsAppMode { get; set; } = WhatsAppMode.Disabled;
    public bool WhatsAppNotifyJourneyStart { get; set; }
    public bool WhatsAppNotifyCheckIn { get; set; }
    public bool WhatsAppNotifyCompletion { get; set; }
    public bool WhatsAppNotifyFailure { get; set; }

    // YENİ: Twilio/WhatsApp Integration Fields - PUBLIC SETTER'LAR
    [MaxLength(256)] public string? TwilioAccountSid { get; set; }  
    [MaxLength(256)] public string? TwilioAuthToken { get; set; }   
    [MaxLength(50)] public string? TwilioWhatsAppNumber { get; set; } 
    public bool TwilioUseSandbox { get; set; } = false;  
    public bool TwilioVerified { get; set; } = false;    
    public DateTime? TwilioConnectedAt { get; set; }     

    // YENİ: Settings property (JSON column olarak saklanacak)
    [Column(TypeName = "nvarchar(max)")]
    public WorkspaceSettings Settings { get; set; }

    // Navigation Properties
    public ICollection<Depot> Depots { get; set; }
    public ICollection<Vehicle> Vehicles { get; set; }
    public ICollection<Customer> Customers { get; set; }
    public ICollection<Driver> Drivers { get; set; }
    public ICollection<Route> Routes { get; set; }
    public ICollection<ApplicationUser> Users { get; set; }
    public ICollection<SavedLocation> SavedLocations { get; set; }
    public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<PaymentMethodEntity> PaymentMethods { get; set; } = new List<PaymentMethodEntity>();
}

// YENİ: Plan Type Enum (aynı namespace içinde)
public enum PlanType
{
    Trial = 0,
    Starter = 1,
    Growth = 2,
    Professional = 3,
    Business = 4
}
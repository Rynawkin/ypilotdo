using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Monolith.WebAPI.Applications.Events;
using Monolith.WebAPI.Applications.Commands.Members;
using Monolith.WebAPI.Applications.Commands.Workspace;
using Monolith.WebAPI.Data.Members.Events;
using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.Infrastructure;

namespace Monolith.WebAPI.Data.Members;

public partial class ApplicationUser : IdentityUser<Guid>
{
    // do not remove this constructor
    public ApplicationUser()
    {
    }

    public ApplicationUser(CreateWorkspaceCommand command, Workspace.Workspace workspace)
    {
        UserName = command.AdminUserEmail;
        Email = command.AdminUserEmail;
        FullName = command.AdminUserFullName;
        IsDriver = false;
        IsAdmin = true;
        IsSuperAdmin = false;
        WorkspaceId = workspace.Id;
        Workspace = workspace;
    }

    public ApplicationUser(TempMember tempMember)
    {
        UserName = tempMember.Email;
        Email = tempMember.Email;
        FullName = tempMember.FullName;
        IsDriver = tempMember.Roles.Contains((int) MemberRole.Driver);
        IsAdmin = tempMember.Roles.Contains((int) MemberRole.Admin);
        IsDispatcher = tempMember.Roles.Contains((int) MemberRole.Dispatcher);
        IsSuperAdmin = false;
        WorkspaceId = tempMember.WorkspaceId;
        DepotId = tempMember.DepotId;
        IsOnboarded = false;
    }

    // SEED İÇİN FACTORY METHOD
    public static ApplicationUser CreateForSeed(
        string email, 
        string fullName, 
        int workspaceId,
        bool isAdmin = false,
        bool isDispatcher = false,
        bool isDriver = false,
        bool isSuperAdmin = false)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            NormalizedEmail = email.ToUpper(),
            NormalizedUserName = email.ToUpper(),
            SecurityStamp = Guid.NewGuid().ToString()
        };
        
        // Private setter'ları reflection ile set et
        typeof(ApplicationUser).GetProperty(nameof(FullName))
            ?.SetValue(user, fullName);
        typeof(ApplicationUser).GetProperty(nameof(WorkspaceId))
            ?.SetValue(user, workspaceId);
        typeof(ApplicationUser).GetProperty(nameof(IsAdmin))
            ?.SetValue(user, isAdmin);
        typeof(ApplicationUser).GetProperty(nameof(IsDispatcher))
            ?.SetValue(user, isDispatcher);
        typeof(ApplicationUser).GetProperty(nameof(IsDriver))
            ?.SetValue(user, isDriver);
        typeof(ApplicationUser).GetProperty(nameof(IsSuperAdmin))
            ?.SetValue(user, isSuperAdmin);
        typeof(ApplicationUser).GetProperty(nameof(IsOnboarded))
            ?.SetValue(user, true);
        typeof(ApplicationUser).GetProperty(nameof(DriverStatus))
            ?.SetValue(user, "available");
            
        return user;
    }

    [MaxLength(100)] public string FullName { get; private set; }

    public void SetFullName(string fullName)
    {
        FullName = fullName;
        AddDomainEvent(new UserUpdatedEvent(Id));
    }

    [MaxLength(100)] public string LicenseNumber { get; private set; }

    public void SetLicenseNumber(string licenseNumber)
    {
        LicenseNumber = licenseNumber;
        AddDomainEvent(new UserUpdatedEvent(Id));
    }

    public bool IsDriver { get; private set; }

    public void SetDriver(bool isDriver)
    {
        IsDriver = isDriver;
        AddDomainEvent(new UserUpdatedEvent(Id));
    }

    public bool IsDispatcher { get; private set; }

    public void SetDispatcher(bool isDispatcher)
    {
        IsDispatcher = isDispatcher;
        AddDomainEvent(new UserUpdatedEvent(Id));
    }

    public bool IsAdmin { get; private set; }

    public void SetAdmin(bool isAdmin)
    {
        IsAdmin = isAdmin;
        AddDomainEvent(new UserUpdatedEvent(Id));
    }

    // SUPER ADMIN FIELD
    public bool IsSuperAdmin { get; private set; }

    public void SetSuperAdmin(bool isSuperAdmin)
    {
        IsSuperAdmin = isSuperAdmin;
        AddDomainEvent(new UserUpdatedEvent(Id));
    }

    public bool IsOnboarded { get; private set; }
    public int? DepotId { get; private set; }

    public void SetDepotId(int? depotId)
    {
        DepotId = depotId;
        AddDomainEvent(new UserUpdatedEvent(Id));
    }

    public Depot Depot { get; private set; }

    public void SetDepot(Depot depot)
    {
        Depot = depot;
        DepotId = depot?.Id;
        AddDomainEvent(new UserUpdatedEvent(Id));
    }

    public int WorkspaceId { get; private set; }
    public Workspace.Workspace Workspace { get; set; }

    // ========== USER DATA FOR SETTINGS ==========
    
    [MaxLength(2000)]
    public string UserData { get; set; } // JSON field for user-specific settings like theme
    
    // ========== DRIVER SPECIFIC PROPERTIES ==========
    
    [MaxLength(20)]
    public string DriverStatus { get; private set; } = "available";

    public void SetDriverStatus(string status)
    {
        DriverStatus = status;
        AddDomainEvent(new UserUpdatedEvent(Id));
    }

    public double? CurrentLatitude { get; private set; }
    public double? CurrentLongitude { get; private set; }

    public void SetCurrentLocation(double? latitude, double? longitude)
    {
        CurrentLatitude = latitude;
        CurrentLongitude = longitude;
        AddDomainEvent(new UserUpdatedEvent(Id));
    }

    public decimal? Rating { get; private set; }

    public void SetRating(decimal? rating)
    {
        Rating = rating;
        AddDomainEvent(new UserUpdatedEvent(Id));
    }

    public int TotalDeliveries { get; private set; }

    public void SetTotalDeliveries(int totalDeliveries)
    {
        TotalDeliveries = totalDeliveries;
        AddDomainEvent(new UserUpdatedEvent(Id));
    }

    public void IncrementTotalDeliveries()
    {
        TotalDeliveries++;
        AddDomainEvent(new UserUpdatedEvent(Id));
    }

    // Vehicle assignment for drivers
    public int? AssignedVehicleId { get; private set; }
    public Vehicle AssignedVehicle { get; set; }

    public void SetAssignedVehicle(int? vehicleId)
    {
        AssignedVehicleId = vehicleId;
        AddDomainEvent(new UserUpdatedEvent(Id));
    }

    // ========== END OF DRIVER SPECIFIC PROPERTIES ==========

    // ✅ YENİ: Daha esnek yetkilendirme metodları
    public void ThrowIfNotSuperAdmin()
    {
        if (!IsSuperAdmin)
            throw new ApiException("You are not authorized to perform this action. Super Admin access required.", 403);
    }

    public void ThrowIfNotAdmin()
    {
        if (!CanAccessAdminFeatures())
            throw new ApiException("You are not authorized to perform this action. Admin access required.", 403);
    }

    public void ThrowIfNotDispatcher()
    {
        if (!CanAccessDispatcherFeatures())
            throw new ApiException("You are not authorized to perform this action. Dispatcher access required.", 403);
    }

    public void ThrowIfNotDriver()
    {
        if (!CanAccessDriverFeatures())
            throw new ApiException("You are not authorized to perform this action. Driver access required.", 403);
    }

    // ✅ YENİ: Workspace kaynaklarına erişim kontrolü
    public void ThrowIfCannotAccessWorkspaceResources()
    {
        // Workspace kaynaklarına (customers, drivers, vehicles vb.) erişim için
        // en az Dispatcher yetkisi gerekir
        if (!CanAccessDispatcherFeatures())
            throw new ApiException("You are not authorized to access workspace resources.", 403);
    }

    // Rol hiyerarşisi kontrolü - BUNLAR ZATEN DOĞRU
    public bool CanAccessDriverFeatures() => IsDriver || IsDispatcher || IsAdmin || IsSuperAdmin;
    public bool CanAccessDispatcherFeatures() => IsDispatcher || IsAdmin || IsSuperAdmin;
    public bool CanAccessAdminFeatures() => IsAdmin || IsSuperAdmin;
    public bool CanAccessSuperAdminFeatures() => IsSuperAdmin;

    private List<NotificationEvent> _domainEvents;

    [JsonIgnore] public IReadOnlyCollection<NotificationEvent> DomainEvents => _domainEvents?.AsReadOnly();

    private void AddDomainEvent(NotificationEvent eventItem)
    {
        _domainEvents ??= [];

        if (eventItem != null)
            _domainEvents.Add(eventItem);
    }

    public void ClearDomainEvents()
    {
        _domainEvents?.Clear();
    }
}

public class ApplicationRole : IdentityRole<Guid>;
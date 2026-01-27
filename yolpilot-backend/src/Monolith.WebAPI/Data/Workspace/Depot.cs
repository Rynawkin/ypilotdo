using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Monolith.WebAPI.Applications.Commands.Workspace;
using Monolith.WebAPI.Data.Members;
using Monolith.WebAPI.Data.Workspace.Events;

namespace Monolith.WebAPI.Data.Workspace;

public class Depot : BaseEntity
{
    // do not remove this constructor
    public Depot()
    {
    }

    public Depot(CreateDepotCommand command, int workspaceId)
    {
        Name = command.Name;
        Address = command.Address;
        Latitude = command.Latitude;
        Longitude = command.Longitude;
        StartWorkingHours = command.StartWorkingHours;
        EndWorkingHours = command.EndWorkingHours;
        WorkspaceId = workspaceId;
        IsDefault = false; // Yeni eklenen field
        
        // WorkingHours JSON olarak saklanacak (opsiyonel - frontend uyumu için)
        if (command.StartWorkingHours != TimeSpan.Zero || command.EndWorkingHours.HasValue)
        {
            var workingHours = CreateDefaultWorkingHours(command.StartWorkingHours, command.EndWorkingHours);
            WorkingHoursJson = JsonSerializer.Serialize(workingHours);
        }

        AddDomainEvent(new SavedLocationCreatedEvent(Id, Name));
    }

    public void Update(UpdateDepotCommand command)
    {
        Name = command.Name;
        Address = command.Address;
        Latitude = command.Latitude;
        Longitude = command.Longitude;
        StartWorkingHours = command.StartWorkingHours;
        EndWorkingHours = command.EndWorkingHours;
        
        // WorkingHours JSON güncelle
        if (command.StartWorkingHours != TimeSpan.Zero || command.EndWorkingHours.HasValue)
        {
            var workingHours = CreateDefaultWorkingHours(command.StartWorkingHours, command.EndWorkingHours);
            WorkingHoursJson = JsonSerializer.Serialize(workingHours);
        }

        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new SavedLocationUpdatedEvent(Id, Name));
    }
    
    // IsDefault için setter method
    public void SetAsDefault(bool isDefault)
    {
        IsDefault = isDefault;
    }

    [MaxLength(128)] public string Name { get; private set; }
    [MaxLength(1024)] public string Address { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }

    public TimeSpan StartWorkingHours { get; private set; }
    public TimeSpan? EndWorkingHours { get; private set; }
    
    // Yeni eklenen field
    public bool IsDefault { get; private set; }
    
    // WorkingHours JSON olarak saklanacak (Frontend uyumu için)
    [Column("WorkingHoursJson")]
    public string WorkingHoursJson { get; private set; }
    
    // WorkingHours property'si - JSON'dan deserialize edilecek
    [NotMapped]
    public Dictionary<string, WorkingHourDto> WorkingHours
    {
        get
        {
            if (string.IsNullOrEmpty(WorkingHoursJson))
                return CreateDefaultWorkingHours(StartWorkingHours, EndWorkingHours);
            
            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, WorkingHourDto>>(WorkingHoursJson) 
                       ?? CreateDefaultWorkingHours(StartWorkingHours, EndWorkingHours);
            }
            catch
            {
                return CreateDefaultWorkingHours(StartWorkingHours, EndWorkingHours);
            }
        }
    }

    public int WorkspaceId { get; private set; }
    public Workspace Workspace { get; set; }

    public ICollection<ApplicationUser> Members { get; set; }
    
    // Helper method to create default working hours
    private static Dictionary<string, WorkingHourDto> CreateDefaultWorkingHours(TimeSpan start, TimeSpan? end)
    {
        var workingHours = new Dictionary<string, WorkingHourDto>();
        var days = new[] { "monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday" };
        
        foreach (var day in days)
        {
            if (day == "saturday" || day == "sunday")
            {
                workingHours[day] = new WorkingHourDto { Open = "closed", Close = "closed" };
            }
            else
            {
                workingHours[day] = new WorkingHourDto 
                { 
                    Open = start.ToString(@"hh\:mm"),
                    Close = end?.ToString(@"hh\:mm") ?? "18:00"
                };
            }
        }
        
        return workingHours;
    }
}

// WorkingHour DTO for JSON serialization
public class WorkingHourDto
{
    public string Open { get; set; }
    public string Close { get; set; }
}
using System.ComponentModel.DataAnnotations;
using Monolith.WebAPI.Applications.Commands.Workspace;
using Monolith.WebAPI.Data.Workspace.Events;

namespace Monolith.WebAPI.Data.Workspace;

public class SavedLocation : BaseEntity
{
    // do not remove this constructor
    public SavedLocation()
    {
    }

    public SavedLocation(CreateSavedLocationCommand command, int workspaceId)
    {
        Name = command.Name;
        Address = command.Address;
        Latitude = command.Latitude;
        Longitude = command.Longitude;
        WorkspaceId = workspaceId;

        AddDomainEvent(new SavedLocationCreatedEvent(Id, Name));
    }

    public void Update(UpdateSavedLocationCommand command)
    {
        Name = command.Name;
        Address = command.Address;
        Latitude = command.Latitude;
        Longitude = command.Longitude;

        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new SavedLocationUpdatedEvent(Id, Name));
    }


    [MaxLength(128)] public string Name { get; private set; }
    [MaxLength(1024)] public string Address { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }

    public int WorkspaceId { get; private set; }
    public Workspace Workspace { get; set; }
}
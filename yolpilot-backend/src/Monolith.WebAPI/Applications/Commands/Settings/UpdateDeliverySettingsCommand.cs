// src/Monolith.WebAPI/Applications/Commands/Settings/UpdateDeliverySettingsCommand.cs

using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.Services.Members;
using System.Text.Json;

namespace Monolith.WebAPI.Applications.Commands.Settings;

public class UpdateDeliverySettingsCommand : BaseAuthenticatedCommand<Unit>
{
    public override bool RequiresDispatcher => true;
    
    public int DefaultServiceTime { get; set; }
    public int MaxDeliveriesPerRoute { get; set; }
    public bool DefaultSignatureRequired { get; set; }
    public bool DefaultPhotoRequired { get; set; }
    
    // Açıkça Responses.Settings namespace'ini belirt
    public Dictionary<string, Monolith.WebAPI.Responses.Settings.WorkingHours> WorkingHours { get; set; }
    public Monolith.WebAPI.Responses.Settings.PrioritySettings PrioritySettings { get; set; }
    
    public bool AutoOptimize { get; set; }
    public bool TrafficConsideration { get; set; }
    public double? CostPerKm { get; set; }
    public double? CostPerHour { get; set; }
}

public class UpdateDeliverySettingsCommandHandler : BaseAuthenticatedCommandHandler<UpdateDeliverySettingsCommand, Unit>
{
    private readonly AppDbContext _context;

    public UpdateDeliverySettingsCommandHandler(IUserService userService, AppDbContext context) 
        : base(userService)
    {
        _context = context;
    }

    protected override async Task<Unit> HandleCommand(UpdateDeliverySettingsCommand request, CancellationToken cancellationToken)
    {
        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == User.WorkspaceId, cancellationToken);

        if (workspace == null)
            throw new Exception("Workspace not found");

        workspace.DefaultServiceTime = TimeSpan.FromMinutes(request.DefaultServiceTime);
        
        // Settings nesnesini güncelle (direkt nesne olarak)
        if (workspace.Settings == null)
        {
            workspace.Settings = new WorkspaceSettings();
        }
        
        // Delivery settings'i güncelle
        workspace.Settings.MaxDeliveriesPerRoute = request.MaxDeliveriesPerRoute;
        workspace.Settings.DefaultSignatureRequired = request.DefaultSignatureRequired;
        workspace.Settings.DefaultPhotoRequired = request.DefaultPhotoRequired;
        workspace.Settings.AutoOptimize = request.AutoOptimize;
        workspace.Settings.TrafficConsideration = request.TrafficConsideration;
        
        // Working hours ve Priority settings'i sadece JSON string kolonlarına kaydet
        // (WorkspaceSettings içinde bunlar farklı tipte olabilir, o yüzden sadece string kolonları güncelliyoruz)
        if (request.WorkingHours != null)
        {
            workspace.WorkingHours = JsonSerializer.Serialize(request.WorkingHours);
        }
        
        if (request.PrioritySettings != null)
        {
            workspace.PrioritySettings = JsonSerializer.Serialize(request.PrioritySettings);
        }
        
        // Cost settings
        workspace.CostPerKm = request.CostPerKm;
        workspace.CostPerHour = request.CostPerHour;
        
        workspace.UpdatedAt = DateTime.UtcNow;
        
        // Entity'yi explicit olarak modified işaretle
        _context.Entry(workspace).State = EntityState.Modified;
        _context.Entry(workspace).Property(x => x.Settings).IsModified = true;
        
        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
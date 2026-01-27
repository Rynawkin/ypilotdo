// src/Monolith.WebAPI/Applications/Commands/Settings/UpdateWorkspaceSettingsCommand.cs

using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.Services.Members;
using System.Text.Json;

namespace Monolith.WebAPI.Applications.Commands.Settings;

public class UpdateWorkspaceSettingsCommand : BaseAuthenticatedCommand<Unit>
{
    public override bool RequiresAdmin => true;
    
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

public class UpdateWorkspaceSettingsCommandHandler : BaseAuthenticatedCommandHandler<UpdateWorkspaceSettingsCommand, Unit>
{
    private readonly AppDbContext _context;

    public UpdateWorkspaceSettingsCommandHandler(IUserService userService, AppDbContext context) 
        : base(userService)
    {
        _context = context;
    }

    protected override async Task<Unit> HandleCommand(UpdateWorkspaceSettingsCommand request, CancellationToken cancellationToken)
    {
        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == User.WorkspaceId, cancellationToken);

        if (workspace == null)
            throw new Exception("Workspace not found");

        // Update all fields
        if (!string.IsNullOrEmpty(request.Name))
            workspace.Name = request.Name;
        if (!string.IsNullOrEmpty(request.Address))
            workspace.Address = request.Address;
        if (!string.IsNullOrEmpty(request.City))
            workspace.City = request.City;
        if (!string.IsNullOrEmpty(request.PostalCode))
            workspace.PostalCode = request.PostalCode;
        if (!string.IsNullOrEmpty(request.TaxNumber))
            workspace.TaxNumber = request.TaxNumber;
        if (!string.IsNullOrEmpty(request.PhoneNumber))
            workspace.PhoneNumber = request.PhoneNumber;
        if (!string.IsNullOrEmpty(request.Email))
            workspace.Email = request.Email;
        if (!string.IsNullOrEmpty(request.Website))
            workspace.Website = request.Website;
        if (!string.IsNullOrEmpty(request.Currency))
            workspace.Currency = request.Currency;
        if (!string.IsNullOrEmpty(request.TimeZone))
            workspace.TimeZone = request.TimeZone;
        if (!string.IsNullOrEmpty(request.Language))
            workspace.Language = request.Language;
        if (!string.IsNullOrEmpty(request.DateFormat))
            workspace.DateFormat = request.DateFormat;
        if (!string.IsNullOrEmpty(request.FirstDayOfWeek))
            workspace.FirstDayOfWeek = request.FirstDayOfWeek;

        workspace.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
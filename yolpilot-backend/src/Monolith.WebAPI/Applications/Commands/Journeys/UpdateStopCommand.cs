using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Journeys;
using Monolith.WebAPI.Hubs;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Services.Members;
using Monolith.WebAPI.Services.Storage;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Reflection;

namespace Monolith.WebAPI.Applications.Commands.Journeys;

public class UpdateStopCommand : BaseAuthenticatedCommand<bool>
{
    // Route stop güncellemesi için Dispatcher, Journey stop için Driver yeterli
    [JsonIgnore] public override bool RequiresWorkspaceAccess => true;
    
    public int StopId { get; set; }
    public int? RouteId { get; set; }
    public int? CustomerId { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Notes { get; set; }
    public string ContactFullName { get; set; }
    public string ContactPhone { get; set; }
    public string ContactEmail { get; set; }
    public LocationType Type { get; set; }
    public OrderType OrderType { get; set; }
    public bool ProofOfDeliveryRequired { get; set; }
    public bool? SignatureRequired { get; set; }
    public bool? PhotoRequired { get; set; }
    public TimeSpan? ArriveBetweenStart { get; set; }
    public TimeSpan? ArriveBetweenEnd { get; set; }
    public TimeSpan? ServiceTime { get; set; }
    
    // For journey stop updates
    public JourneyStopStatus? Status { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string Photo { get; set; }
    public string Signature { get; set; }
}

public class UpdateStopCommandValidator : AbstractValidator<UpdateStopCommand>
{
    public UpdateStopCommandValidator()
    {
        RuleFor(x => x.StopId).GreaterThan(0);
        RuleFor(x => x.Name).MaximumLength(128).When(x => !string.IsNullOrEmpty(x.Name));
        RuleFor(x => x.Address).MaximumLength(1024).When(x => !string.IsNullOrEmpty(x.Address));
        RuleFor(x => x.Notes).MaximumLength(1024).When(x => !string.IsNullOrEmpty(x.Notes));
        RuleFor(x => x.ContactFullName).MaximumLength(128).When(x => !string.IsNullOrEmpty(x.ContactFullName));
        RuleFor(x => x.ContactPhone).MaximumLength(16).When(x => !string.IsNullOrEmpty(x.ContactPhone));
        RuleFor(x => x.ContactEmail).MaximumLength(64).EmailAddress().When(x => !string.IsNullOrEmpty(x.ContactEmail));
        
        // Sadece basic validation - time window manipulation Handler'da yapılacak
        RuleFor(x => x)
            .Custom((x, context) =>
            {
                // Her iki değer de varsa, mantıklı olduğunu kontrol et
                if (x.ArriveBetweenStart.HasValue && x.ArriveBetweenEnd.HasValue)
                {
                    if (x.ArriveBetweenStart.Value >= x.ArriveBetweenEnd.Value)
                    {
                        context.AddFailure("ArriveBetweenEnd", "Bitiş saati, başlangıç saatinden sonra olmalıdır.");
                    }
                }
            });
    }
}

public class UpdateStopCommandHandler : BaseAuthenticatedCommandHandler<UpdateStopCommand, bool>
{
    private readonly AppDbContext _context;
    private readonly IBlobStorageService _blobStorage;
    private readonly IHubContext<JourneyHub> _hubContext;

    public UpdateStopCommandHandler(
        AppDbContext context,
        IBlobStorageService blobStorage,
        IHubContext<JourneyHub> hubContext,
        IUserService userService)
        : base(userService)
    {
        _context = context;
        _blobStorage = blobStorage;
        _hubContext = hubContext;
    }

    protected override async Task<bool> HandleCommand(UpdateStopCommand request, CancellationToken cancellationToken)
    {
        // Time window auto-completion (validator'da yapılmıyor ise burada da yapabiliriz)
        if (request.ArriveBetweenStart.HasValue && !request.ArriveBetweenEnd.HasValue)
        {
            request.ArriveBetweenEnd = request.ArriveBetweenStart.Value.Add(TimeSpan.FromHours(1));
        }
        else if (!request.ArriveBetweenStart.HasValue && request.ArriveBetweenEnd.HasValue)
        {
            request.ArriveBetweenStart = request.ArriveBetweenEnd.Value.Subtract(TimeSpan.FromHours(1));
            if (request.ArriveBetweenStart.Value < TimeSpan.Zero)
            {
                request.ArriveBetweenStart = TimeSpan.FromMinutes(1);
            }
        }
        
        // 00:00 edge case düzeltmesi
        if (request.ArriveBetweenStart.HasValue && request.ArriveBetweenStart.Value.TotalMinutes == 0)
        {
            request.ArriveBetweenStart = TimeSpan.FromMinutes(1);
        }
        
        // Check if this is a journey stop update
        if (request.Status.HasValue)
        {
            // Journey stop güncellemesi için Driver yeterli
            if (!User.IsDriver && !User.IsDispatcher && !User.IsAdmin && !User.IsSuperAdmin)
            {
                throw new ApiException("You need at least Driver role to update journey stops.", 403);
            }
            return await HandleJourneyStopUpdate(request, cancellationToken);
        }
        
        // Route stop güncellemesi için Dispatcher gerekli
        if (!User.IsDispatcher && !User.IsAdmin && !User.IsSuperAdmin)
        {
            throw new ApiException("You need at least Dispatcher role to update route stops.", 403);
        }
        return await HandleRouteStopUpdate(request, cancellationToken);
    }
    
    private async Task<bool> HandleJourneyStopUpdate(UpdateStopCommand request, CancellationToken cancellationToken)
    {
        var stop = await _context.JourneyStops
            .Include(s => s.Journey)
            .Include(s => s.RouteStop)
            .FirstOrDefaultAsync(s => s.Id == request.StopId, cancellationToken);

        if (stop == null)
            throw new ApiException($"Journey stop with ID {request.StopId} not found", 404);

        if (request.Status == JourneyStopStatus.Completed)
        {
            // İmza kontrolü
            if (stop.RouteStop?.SignatureRequired == true && string.IsNullOrEmpty(request.Signature))
            {
                throw new ApiException("Bu teslimat için imza zorunludur.", 400);
            }
            
            // Fotoğraf kontrolü
            if (stop.RouteStop?.PhotoRequired == true && string.IsNullOrEmpty(request.Photo))
            {
                throw new ApiException("Bu teslimat için fotoğraf zorunludur.", 400);
            }
        }
        
        // Upload images to blob storage
        string photoUrl = null;
        string signatureUrl = null;
        
        if (!string.IsNullOrEmpty(request.Photo))
        {
            var photoBytes = Convert.FromBase64String(request.Photo.Split(',')[1]);
            using var photoStream = new MemoryStream(photoBytes);
            photoUrl = await _blobStorage.UploadAsync(photoStream, $"photo_{stop.Id}.jpg", "image/jpeg");
        }

        if (!string.IsNullOrEmpty(request.Signature))
        {
            var signatureBytes = Convert.FromBase64String(request.Signature.Split(',')[1]);
            using var signatureStream = new MemoryStream(signatureBytes);
            signatureUrl = await _blobStorage.UploadAsync(signatureStream, $"signature_{stop.Id}.png", "image/png");
        }

        // Update stop
        stop.Status = request.Status.Value;
        stop.CheckInTime = request.CheckInTime;
        stop.CheckOutTime = request.CheckOutTime;
        stop.UpdatedAt = DateTime.UtcNow;
        
        // Add journey status with URLs
        var journeyStatus = new JourneyStatus(
            stop.JourneyId,
            stop.Id,
            request.Status.Value == JourneyStopStatus.Completed ? JourneyStatusType.Completed : JourneyStatusType.Processing)
        {
            SignatureUrl = signatureUrl,
            PhotoUrl = photoUrl
        };
        
        _context.JourneyStatuses.Add(journeyStatus);
        await _context.SaveChangesAsync(cancellationToken);

        // SignalR notification - Real-time update
        await _hubContext.Clients.Group($"journey-{stop.JourneyId}")
            .SendAsync("StopCompleted", stop.JourneyId, stop.Id, new
            {
                stopId = stop.Id,
                status = stop.Status.ToString(),
                checkInTime = stop.CheckInTime,
                checkOutTime = stop.CheckOutTime,
                photoUrl = photoUrl,
                signatureUrl = signatureUrl
            }, cancellationToken);

        // Workspace notification
        await _hubContext.Clients.Group($"workspace-{User.WorkspaceId}")
            .SendAsync("JourneyUpdated", stop.JourneyId, cancellationToken);

        return true;
    }
    
    private async Task<bool> HandleRouteStopUpdate(UpdateStopCommand request, CancellationToken cancellationToken)
    {
        Console.WriteLine("=== HANDLEROUTE STOP UPDATE DEBUG ===");
        Console.WriteLine($"StopId: {request.StopId}");
        Console.WriteLine($"Request ArriveBetweenStart: {request.ArriveBetweenStart}");
        Console.WriteLine($"Request ArriveBetweenEnd: {request.ArriveBetweenEnd}");
        Console.WriteLine($"Request ServiceTime: {request.ServiceTime}");

        var routeStop = await _context.RouteStops
            .FirstOrDefaultAsync(s => s.Id == request.StopId, cancellationToken);

        if (routeStop == null)
            throw new ApiException($"Route stop with ID {request.StopId} not found", 404);

        Console.WriteLine($"BEFORE UPDATE - DB ArriveBetweenStart: {routeStop.ArriveBetweenStart}");
        Console.WriteLine($"BEFORE UPDATE - DB ArriveBetweenEnd: {routeStop.ArriveBetweenEnd}");

        // Use the Update method instead of reflection (like the working backup version)
        routeStop.Update(request);

        Console.WriteLine($"AFTER UPDATE - Entity ArriveBetweenStart: {routeStop.ArriveBetweenStart}");
        Console.WriteLine($"AFTER UPDATE - Entity ArriveBetweenEnd: {routeStop.ArriveBetweenEnd}");

        // Mark as modified to ensure EF Core tracks the changes
        _context.Entry(routeStop).State = EntityState.Modified;
        _context.Entry(routeStop).Property(x => x.ArriveBetweenStart).IsModified = true;
        _context.Entry(routeStop).Property(x => x.ArriveBetweenEnd).IsModified = true;

        Console.WriteLine("Marked properties as modified");

        var saveResult = await _context.SaveChangesAsync(cancellationToken);
        Console.WriteLine($"DATABASE SAVE COMPLETED - Affected rows: {saveResult}");

        // Reload and verify
        await _context.Entry(routeStop).ReloadAsync(cancellationToken);
        Console.WriteLine($"AFTER RELOAD - DB ArriveBetweenStart: {routeStop.ArriveBetweenStart}");
        Console.WriteLine($"AFTER RELOAD - DB ArriveBetweenEnd: {routeStop.ArriveBetweenEnd}");
        Console.WriteLine("=== END DEBUG ===");

        return true;
    }
}
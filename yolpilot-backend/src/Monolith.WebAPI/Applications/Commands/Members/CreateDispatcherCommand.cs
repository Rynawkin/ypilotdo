// src/Monolith.WebAPI/Applications/Commands/Members/CreateDispatcherCommand.cs

using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Members;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Responses.Members;
using Monolith.WebAPI.Services.Email;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Members;

public class CreateDispatcherCommand : BaseAuthenticatedCommand<UserResponse>
{
    [JsonIgnore] public override bool RequiresAdmin => true;
    
    public string FullName { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string PhoneNumber { get; set; }
    public int? DepotId { get; set; }
}

public class CreateDispatcherCommandValidator : AbstractValidator<CreateDispatcherCommand>
{
    public CreateDispatcherCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().WithMessage("Ad Soyad zorunludur");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Geçerli bir email adresi giriniz");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalıdır");
    }
}

public class CreateDispatcherCommandHandler : BaseAuthenticatedCommandHandler<CreateDispatcherCommand, UserResponse>
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<CreateDispatcherCommandHandler> _logger;

    public CreateDispatcherCommandHandler(
        AppDbContext context,
        IUserService userService,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        ILogger<CreateDispatcherCommandHandler> logger) : base(userService)
    {
        _context = context;
        _userManager = userManager;
        _emailService = emailService;
        _logger = logger;
    }

    protected override async Task<UserResponse> HandleCommand(CreateDispatcherCommand request, CancellationToken cancellationToken)
    {
        // Email kontrolü
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new ApiException($"Bu email adresi zaten kullanımda: {request.Email}", 400);
        }

        // Depot kontrolü
        if (request.DepotId.HasValue)
        {
            var depot = await _context.Depots
                .FirstOrDefaultAsync(d => d.Id == request.DepotId.Value && d.WorkspaceId == User.WorkspaceId, cancellationToken);
            
            if (depot == null)
                throw new ApiException("Depot bulunamadı", 404);
        }

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
            // ApplicationUser oluştur
            var applicationUser = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                EmailConfirmed = true,
                PhoneNumber = request.PhoneNumber,
                PhoneNumberConfirmed = false
            };

            // Private setter'ları reflection ile set et
            typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.FullName))
                ?.SetValue(applicationUser, request.FullName);
            typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.WorkspaceId))
                ?.SetValue(applicationUser, User.WorkspaceId);
            typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.IsDispatcher))
                ?.SetValue(applicationUser, true);
            typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.IsDriver))
                ?.SetValue(applicationUser, false);
            typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.IsAdmin))
                ?.SetValue(applicationUser, false);
            typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.IsSuperAdmin))
                ?.SetValue(applicationUser, false);
            typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.IsOnboarded))
                ?.SetValue(applicationUser, true);
            
            if (request.DepotId.HasValue)
            {
                typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.DepotId))
                    ?.SetValue(applicationUser, request.DepotId.Value);
            }

            // UserManager ile kullanıcıyı oluştur
            var createUserResult = await _userManager.CreateAsync(applicationUser, request.Password);
            
            if (!createUserResult.Succeeded)
            {
                var errors = string.Join(", ", createUserResult.Errors.Select(e => e.Description));
                throw new ApiException($"Kullanıcı oluşturulamadı: {errors}", 400);
            }

            // Email gönder (opsiyonel)
            try
            {
                await _emailService.SendWelcomeEmailToDispatcher(
                    applicationUser.Email,
                    request.FullName,
                    User.Workspace?.Name ?? "RotaApp"
                );
                _logger.LogInformation($"Welcome email sent to dispatcher: {applicationUser.Email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Welcome email could not be sent: {applicationUser.Email}");
            }

            await transaction.CommitAsync(cancellationToken);
            
            return new UserResponse(applicationUser);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Members;
using Monolith.WebAPI.Data.Seed;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Workspace;

public class CreateWorkspaceCommand : IRequest<TokenModel>
{
    public string WorkspaceName { get; set; }

    public string AdminUserFullName { get; set; }
    public string AdminUserEmail { get; set; }
    public string AdminUserPassword { get; set; }

    public Data.Workspace.Workspace ToEntity() => new(this);

    public ApplicationUser ToUserEntity(Data.Workspace.Workspace workspace) => new(this, workspace);
}

public class CreateWorkspaceCommandValidator : AbstractValidator<CreateWorkspaceCommand>
{
    public CreateWorkspaceCommandValidator()
    {
        RuleFor(x => x.WorkspaceName).NotEmpty();
        RuleFor(x => x.AdminUserEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.AdminUserPassword).NotEmpty();
    }
}

public class CreateWorkspaceCommandHandler : IRequestHandler<CreateWorkspaceCommand, TokenModel>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly AppDbContext _context;

    public CreateWorkspaceCommandHandler(
        UserManager<ApplicationUser> userManager, 
        ITokenService tokenService,
        AppDbContext context)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _context = context;
    }

    public async Task<TokenModel> Handle(CreateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var workspace = request.ToEntity();
        
        // User'ı workspace ile birlikte oluştur
        var user = request.ToUserEntity(workspace);
        
        var result = await _userManager.CreateAsync(user, request.AdminUserPassword);
        if (result.Succeeded is false)
        {
            var errors = result.Errors.Select(x => x.Description);
            throw new ApiException(string.Join(" ", errors), 400);
        }

        // Workspace Id'sini user'dan al (UserManager workspace'i de kaydetmiş olmalı)
        var savedWorkspaceId = user.WorkspaceId;
        
        // Default template'leri ekle
        var defaultTemplates = DefaultTemplates.GetDefaultTemplates(savedWorkspaceId);
        
        if (defaultTemplates != null && defaultTemplates.Any())
        {
            await _context.MessageTemplates.AddRangeAsync(defaultTemplates);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var token = _tokenService.Create(user);
        return token;
    }
}
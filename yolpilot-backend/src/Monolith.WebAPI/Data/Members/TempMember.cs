using System.ComponentModel.DataAnnotations;
using Monolith.WebAPI.Applications.Commands.Members;
using Monolith.WebAPI.Data.Members.Events;
using Monolith.WebAPI.Data.Workspace;

namespace Monolith.WebAPI.Data.Members;

public class TempMember : BaseEntity
{
    // do not remove this constructor
    public TempMember()
    {
    }

    public TempMember(CreateTempMemberCommand command, ApplicationUser inviter, Workspace.Workspace workspace, Depot depot)
    {
        FullName = command.FullName;
        Email = command.Email;
        Roles = command.Roles.Select(x => (int) x).ToArray();
        InviterId = command.AuthenticatedUserId.ToString();
        WorkspaceId = workspace.Id;
        DepotId = command.DepotId;
        Token = Guid.NewGuid();

        AddDomainEvent(new MemberInvitedEvent(inviter, inviter, Email, Token.ToString()));
    }

    public void Save(ApplicationUser inviter, Workspace.Workspace workspace)
    {
        IsSaved = true;

        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new MemberJoinedEvent(inviter.Id, Email, FullName, WorkspaceId));
    }

    public Guid Token { get; private set; }
    [MaxLength(128)] public string FullName { get; private set; }
    [MaxLength(64)] public string Email { get; private set; }
    public int[] Roles { get; private set; }
    public bool IsSaved { get; private set; }


    public string InviterId { get; private set; }
    public int WorkspaceId { get; private set; }
    public int DepotId { get; private set; }
}
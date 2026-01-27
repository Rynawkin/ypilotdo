// src/Monolith.WebAPI/Data/Members/Events/MemberInvitedEvent.cs

using MediatR;
using Monolith.WebAPI.Applications.Events;

namespace Monolith.WebAPI.Data.Members.Events;

public class MemberInvitedEvent : NotificationEvent
{
    public ApplicationUser InvitedBy { get; set; } = null!;
    public ApplicationUser Member { get; set; } = null!;
    public string Email { get; set; } = string.Empty;
    public string InviteCode { get; set; } = string.Empty;
    public bool EmailSent { get; set; }
    public string? EmailError { get; set; }
    public DateTime InvitedAt { get; set; }
    
    public MemberInvitedEvent(ApplicationUser invitedBy, ApplicationUser member, string email, string inviteCode)
    {
        InvitedBy = invitedBy;
        Member = member;
        Email = email;
        InviteCode = inviteCode;
        InvitedAt = DateTime.UtcNow;
        EmailSent = false;
    }
}

public class MemberInvitedEventHandler : INotificationHandler<MemberInvitedEvent>
{
    public async Task Handle(MemberInvitedEvent notification, CancellationToken cancellationToken)
    {
        // TODO: Email service implementation will be added later
        // For now, just log the event
        await Task.CompletedTask;
    }
}
using System.Text;
using Monolith.WebAPI.Applications.Commands.Members;
using Monolith.WebAPI.Data.Journeys.Events;
using Monolith.WebAPI.Data.Members.Events;

namespace Monolith.WebAPI.Services.Email;

public static class EmailTemplates
{
    private const string BaseUrl = "https://domain.com";
    private const string Company = "Planit";

    /// <summary>
    /// Returns the title and content of the email to be sent when a member is invited to the workspace 
    /// </summary>
    public static (string subject, string content) MemberInvited_InvitedEmail(MemberInvitedEvent notification)
    {
        var workspaceName = "Workspace";  // DÜZELTİLDİ - Member'da Name yok, FullName var
        var inviterName = notification.InvitedBy?.FullName ?? "Inviter";
        
        var subject = "TR" switch  // Default to TR for now
        {
            "TR" => $"{inviterName} Sizi {workspaceName} Çalışma Alanına Davet Etti",
            _ => $"{inviterName} Invited You to {workspaceName} Workspace"
        };

        var file = $"./Templates/TR/member_invited.html";
        var fileExist = File.Exists(file);

        var content = fileExist ? File.ReadAllText(file) : GetDefaultInviteTemplate();

        content = content.Replace("{{title}}", subject);
        content = content.Replace("{{confirmationUrl}}", $"{BaseUrl}/confirm-invitation?token={notification.InviteCode}");
        content = content.Replace("{{inviterFullName}}", inviterName);
        content = content.Replace("{{invitedFullName}}", notification.Email);
        content = content.Replace("{{workspaceName}}", workspaceName);
        content = content.Replace("{{depotName}}", "Main Depot");

        return (subject + " - " + Company, content);
    }

    /// <summary>
    /// Returns the title and content of the email to be sent when a member joined the workspace
    /// </summary>
    public static (string subject, string content) MemberJoined_InviterEmail(MemberJoinedEvent notification)
    {
        var subject = "TR" switch  // Default to TR for now
        {
            "TR" => $"Çalışanınız {notification.Name} Katıldı",
            _ => $"{notification.Name} Joined Your Workspace"
        };

        var file = $"./Templates/TR/member_joined.html";
        var fileExist = File.Exists(file);

        var content = fileExist ? File.ReadAllText(file) : GetDefaultJoinedTemplate();

        content = content.Replace("{{title}}", subject);
        content = content.Replace("{{inviterFullName}}", "Admin");
        content = content.Replace("{{invitedFullName}}", notification.Name);
        content = content.Replace("{{workspaceName}}", "Workspace");

        return (subject + " - " + Company, content);
    }

    // JourneyOptimized_DriverEmail METODU KALDIRILDI

    private static string GetDefaultInviteTemplate()
    {
        return """
            <html>
            <body>
                <h1>{{title}}</h1>
                <p>You have been invited to join {{workspaceName}}.</p>
                <p>Click <a href="{{confirmationUrl}}">here</a> to accept the invitation.</p>
            </body>
            </html>
            """;
    }

    private static string GetDefaultJoinedTemplate()
    {
        return """
            <html>
            <body>
                <h1>{{title}}</h1>
                <p>{{invitedFullName}} has joined {{workspaceName}}.</p>
            </body>
            </html>
            """;
    }

    // GetDefaultOptimizedTemplate METODU KALDIRILDI
}
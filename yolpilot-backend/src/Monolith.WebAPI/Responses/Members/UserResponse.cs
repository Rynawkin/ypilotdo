using Monolith.WebAPI.Data.Members;
using Monolith.WebAPI.Responses.Workspace;

namespace Monolith.WebAPI.Responses.Members;

public class UserResponse(ApplicationUser user)
{
    public string Id => user.Id.ToString();

    public string FullName => user.FullName;

    //public string UserName => user.UserName;
    public string Email => user.Email;
    public string PhoneNumber => user.PhoneNumber;
    public bool IsDriver => user.IsDriver;
    public bool IsDispatcher => user.IsDispatcher;
    public bool IsAdmin => user.IsAdmin;
    public bool IsSuperAdmin => user.IsSuperAdmin; // YENİ EKLENDİ
    public bool IsOnboarded => user.IsOnboarded;
    public int WorkspaceId => user.WorkspaceId;
    public int? DepotId => user.DepotId;
    public bool IsRegistered => !(user.Id == Guid.Empty);
    public WorkspaceResponse Workspace => user.Workspace == null ? null : new WorkspaceResponse(user.Workspace);

    public DepotResponse Depot
    {
        get
        {
            if (user.Depot != null)
            {
                user.Depot.Members = null;
                return new DepotResponse(user.Depot);
            }

            return null;
        }
    }
}
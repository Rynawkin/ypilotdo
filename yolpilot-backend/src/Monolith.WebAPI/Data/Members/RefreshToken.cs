namespace Monolith.WebAPI.Data.Members;

public class RefreshToken
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public string Token { get; set; } = Guid.NewGuid().ToString();
    public string BearerToken { get; set; }
}
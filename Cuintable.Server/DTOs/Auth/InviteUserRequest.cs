namespace Cuintable.Server.DTOs.Auth;

public class InviteUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // "Contador" or "Pareja"
    public string PreferredLanguage { get; set; } = "es";
}

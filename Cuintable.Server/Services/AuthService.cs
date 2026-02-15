using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Cuintable.Server.Data;
using Cuintable.Server.DTOs.Auth;
using Cuintable.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Cuintable.Server.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            throw new InvalidOperationException("Email already registered.");

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = $"{request.FullName}'s Tenant"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Role = UserRole.Owner,
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            PreferredLanguage = request.PreferredLanguage
        };

        _db.Tenants.Add(tenant);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return new AuthResponse
        {
            Token = GenerateToken(user),
            Email = user.Email,
            FullName = user.FullName,
            PreferredLanguage = user.PreferredLanguage,
            Role = user.Role.ToString()
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant());

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        return new AuthResponse
        {
            Token = GenerateToken(user),
            Email = user.Email,
            FullName = user.FullName,
            PreferredLanguage = user.PreferredLanguage,
            Role = user.Role.ToString()
        };
    }

    public async Task<AuthResponse> InviteUserAsync(Guid tenantId, InviteUserRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email.ToLowerInvariant()))
            throw new InvalidOperationException("Email already registered.");

        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role) || role == UserRole.Owner)
            throw new InvalidOperationException("Role must be 'Contador' or 'Pareja'.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Role = role,
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            PreferredLanguage = request.PreferredLanguage
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return new AuthResponse
        {
            Token = GenerateToken(user),
            Email = user.Email,
            FullName = user.FullName,
            PreferredLanguage = user.PreferredLanguage,
            Role = user.Role.ToString()
        };
    }

    private string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim("TenantId", user.TenantId.ToString()),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

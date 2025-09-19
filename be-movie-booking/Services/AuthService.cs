using be_movie_booking.Data;
using be_movie_booking.Models;
using be_movie_booking.Repositories;

namespace be_movie_booking.Services;

public interface IAuthService
{
    Task<(User user, string accessToken, DateTime accessExpires, string refreshToken, DateTime refreshExpires)> RegisterAsync(string email, string password, string fullName, string? userAgent, string? ip);
    Task<(User user, string accessToken, DateTime accessExpires, string refreshToken, DateTime refreshExpires)> LoginAsync(string email, string password, string? deviceId, string? userAgent, string? ip);
    Task<(string accessToken, DateTime accessExpires, string refreshToken, DateTime refreshExpires)> RefreshAsync(string refreshToken, string? deviceId, string? userAgent, string? ip);
    Task LogoutAsync(Guid userId);
    Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
}

public class AuthService : IAuthService
{
    private readonly MovieBookingDbContext _db;
    private readonly ITokenService _tokens;
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;

    public AuthService(MovieBookingDbContext db, ITokenService tokens, IUserRepository users, IRefreshTokenRepository refreshTokens)
    {
        _db = db;
        _tokens = tokens;
        _users = users;
        _refreshTokens = refreshTokens;
    }

    public async Task<(User user, string accessToken, DateTime accessExpires, string refreshToken, DateTime refreshExpires)> RegisterAsync(string email, string password, string fullName, string? userAgent, string? ip)
    {
        var exists = await _users.ExistsByEmailAsync(email);
        if (exists) throw new InvalidOperationException("Email already registered");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FullName = fullName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
        };
        await _users.AddAsync(user);
        
        // Assign default "User" role to new user
        var defaultRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111"); // User role
        var userRole = new UserRole
        {
            UserId = user.Id,
            RoleId = defaultRoleId
        };
        _db.UserRoles.Add(userRole);
        
        await _db.SaveChangesAsync();

        var (access, accessExp) = _tokens.CreateAccessTokenAsync(user, new List<string> { "User" }).Result;
        var (refresh, _, refreshExp) = await _tokens.CreateRefreshTokenAsync(user, null, userAgent, ip);
        return (user, access, accessExp, refresh, refreshExp);
    }

    public async Task<(User user, string accessToken, DateTime accessExpires, string refreshToken, DateTime refreshExpires)> LoginAsync(string email, string password, string? deviceId, string? userAgent, string? ip)
    {
        var user = await _users.GetByEmailWithRolesAsync(email) ?? throw new UnauthorizedAccessException();
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) throw new UnauthorizedAccessException();

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var (access, accessExp) = _tokens.CreateAccessTokenAsync(user, roles).Result;
        var (refresh, _, refreshExp) = await _tokens.CreateRefreshTokenAsync(user, deviceId, userAgent, ip);
        return (user, access, accessExp, refresh, refreshExp);
    }

    public async Task<(string accessToken, DateTime accessExpires, string refreshToken, DateTime refreshExpires)> RefreshAsync(string refreshToken, string? deviceId, string? userAgent, string? ip)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) throw new ArgumentException("Invalid refresh token");
        var hash = _tokens.ComputeSha256(refreshToken);
        var rt = await _refreshTokens.GetByHashAsync(hash) ?? throw new UnauthorizedAccessException();
        if (rt.RevokedAt != null || rt.ExpiresAt <= DateTime.UtcNow) throw new UnauthorizedAccessException();

        rt.RevokedAt = DateTime.UtcNow;
        rt.RevokedByIp = ip;
        var user = rt.User;
        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var (access, accessExp) = _tokens.CreateAccessTokenAsync(user, roles).Result;
        var (newRefresh, newHash, refreshExp) = await _tokens.CreateRefreshTokenAsync(user, deviceId ?? rt.DeviceId, userAgent ?? rt.UserAgent, ip);
        rt.ReplacedByTokenHash = newHash;
        await _db.SaveChangesAsync();
        return (access, accessExp, newRefresh, refreshExp);
    }

    
    public async Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        var user = await _users.GetByIdAsync(userId) ?? throw new UnauthorizedAccessException();
        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash)) throw new UnauthorizedAccessException();

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _users.UpdateAsync(user);

        var activeTokens = await _refreshTokens.GetActiveByUserAsync(userId);
        foreach (var t in activeTokens) t.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
    
    public async Task LogoutAsync(Guid userId)
    {
        var tokens = await _refreshTokens.GetActiveByUserAsync(userId);
        foreach (var t in tokens) t.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}

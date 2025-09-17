using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using be_movie_booking.Data;
using be_movie_booking.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace be_movie_booking.Services;

public interface ITokenService
{
    Task<(string accessToken, DateTime expiresAt)> CreateAccessTokenAsync(User user);
    Task<(string refreshToken, string refreshTokenHash, DateTime expiresAt)> CreateRefreshTokenAsync(User user, string? deviceId, string? userAgent, string? ip);
    string ComputeSha256(string input);
}

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;
    private readonly MovieBookingDbContext _db;

    public TokenService(IConfiguration config, MovieBookingDbContext db)
    {
        _config = config;
        _db = db;
    }

    public async Task<(string accessToken, DateTime expiresAt)> CreateAccessTokenAsync(User user)
    {
        var jwtSection = _config.GetSection("Jwt");
        var secret = jwtSection.GetValue<string>("Secret") ?? "dev_secret_change_me";
        var issuer = jwtSection.GetValue<string>("Issuer") ?? "be-movie-booking";
        var audience = jwtSection.GetValue<string>("Audience") ?? "be-movie-booking-client";
        var accessMinutes = jwtSection.GetValue<int?>("AccessTokenMinutes") ?? 15;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(accessMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, user.FullName)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: creds
        );

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return (jwt, expires);
    }

    public async Task<(string refreshToken, string refreshTokenHash, DateTime expiresAt)> CreateRefreshTokenAsync(User user, string? deviceId, string? userAgent, string? ip)
    {
        var refreshDays = _config.GetSection("Jwt").GetValue<int?>("RefreshTokenDays") ?? 14;
        var bytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(bytes);
        var hash = ComputeSha256(token);
        var expires = DateTime.UtcNow.AddDays(refreshDays);

        var entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = hash,
            ExpiresAt = expires,
            CreatedAt = DateTime.UtcNow,
            DeviceId = deviceId,
            UserAgent = userAgent,
            CreatedByIp = ip
        };

        _db.RefreshTokens.Add(entity);
        await _db.SaveChangesAsync();

        return (token, hash, expires);
    }

    public string ComputeSha256(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

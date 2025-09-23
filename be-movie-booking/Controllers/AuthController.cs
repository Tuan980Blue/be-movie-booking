
using be_movie_booking.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace be_movie_booking.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    public record RegisterRequest(string Email, string Password, string FullName);
    public record LoginRequest(string Email, string Password, string? DeviceId, string? UserAgent);
    public record RefreshRequest(string RefreshToken, string? DeviceId, string? UserAgent);
    public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        try
        {
            var result = await _auth.RegisterAsync(req.Email, req.Password, req.FullName, Request.Headers["User-Agent"], HttpContext.Connection.RemoteIpAddress?.ToString());
            return Ok(new { accessToken = result.accessToken, accessTokenExpiresAt = result.accessExpires, refreshToken = result.refreshToken, refreshTokenExpiresAt = result.refreshExpires });
        }
        catch (InvalidOperationException)
        {
            return Conflict("Email already registered");
        }
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        try
        {
            var result = await _auth.LoginAsync(req.Email, req.Password, req.DeviceId, req.UserAgent ?? Request.Headers["User-Agent"], HttpContext.Connection.RemoteIpAddress?.ToString());
            return Ok(new { accessToken = result.accessToken, accessTokenExpiresAt = result.accessExpires, refreshToken = result.refreshToken, refreshTokenExpiresAt = result.refreshExpires });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    [AllowAnonymous]
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshRequest req)
    {
        try
        {
            var result = await _auth.RefreshTokenAsync(req.RefreshToken, req.DeviceId, req.UserAgent ?? Request.Headers["User-Agent"], HttpContext.Connection.RemoteIpAddress?.ToString());
            return Ok(new { accessToken = result.accessToken, accessTokenExpiresAt = result.accessExpires, refreshToken = result.refreshToken, refreshTokenExpiresAt = result.refreshExpires });
        }
        catch
        {
            return Unauthorized();
        }
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        var sub = User?.Claims?.FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                  ?? User?.Claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (sub == null) return Unauthorized();
        try
        {
            await _auth.ChangePasswordAsync(Guid.Parse(sub), req.CurrentPassword, req.NewPassword);
            return Ok();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }
    
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var sub = User?.Claims?.FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                  ?? User?.Claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (sub == null) return Ok();
        await _auth.LogoutAsync(Guid.Parse(sub));
        return Ok();
    }
}

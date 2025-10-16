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

    public record RefreshRequest(string? RefreshToken, string? DeviceId, string? UserAgent);

    public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

    public record LogoutDeviceRequest(string? DeviceId);

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        try
        {
            var result = await _auth.RegisterAsync(
                req.Email,
                req.Password,
                req.FullName,
                Request.Headers["User-Agent"],
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            // Set HttpOnly refresh token cookie
            if (!string.IsNullOrWhiteSpace(result.refreshToken))
            {
                //Append cookie: để thêm hoặc cập nhật một cookie vào phản hồi HTTP gửi đến trình duyệt
                Response.Cookies.Append(
                    "refresh_token", // Tên cookie
                    result.refreshToken, // Giá trị của cookie
                    new CookieOptions
                    {
                        HttpOnly = true, // Cookie chỉ có thể được truy cập bởi server, không qua JavaScript
                        Secure = true, // Cookie chỉ được gửi qua kết nối HTTPS
                        SameSite = SameSiteMode.None, // Chính sách SameSite để kiểm soát việc gửi cookie (none cho phép gửi cookie giữa FE/BE khác domain)
                        Path = "/", // Cookie áp dụng cho toàn bộ đường dẫn trên domain
                        Expires = result.refreshExpires // Thời gian hết hạn của cookie
                    }
                );
                // Thêm cookie flag cho client
                Response.Cookies.Append(
                    "logged_in",
                    "true",
                    new CookieOptions
                    {
                        HttpOnly = false, // để client có thể đọc được
                        Secure = true,
                        SameSite = SameSiteMode.None,
                        Path = "/",
                        Expires = result.refreshExpires
                    }
                );
            }

            // Do not return refresh token in body
            return Ok(new { accessToken = result.accessToken, accessTokenExpiresAt = result.accessExpires });
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
            var result = await _auth.LoginAsync(
                req.Email,
                req.Password,
                req.DeviceId,
                req.UserAgent ?? Request.Headers["User-Agent"],
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            // Set HttpOnly refresh token cookie
            if (!string.IsNullOrWhiteSpace(result.refreshToken))
            {
                Response.Cookies.Append(
                    "refresh_token",
                    result.refreshToken,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.None,
                        Path = "/",
                        Expires = result.refreshExpires
                    }
                );
                // Thêm cookie flag cho client
                Response.Cookies.Append(
                    "logged_in",
                    "true",
                    new CookieOptions
                    {
                        HttpOnly = false, // để client có thể đọc được
                        Secure = true,
                        SameSite = SameSiteMode.None,
                        Path = "/",
                        Expires = result.refreshExpires
                    }
                );
            }

            // Do not return refresh token in body
            return Ok(new { accessToken = result.accessToken, accessTokenExpiresAt = result.accessExpires });
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
            // Prefer refresh token from HttpOnly cookie; fallback to body for backward compatibility
            var cookieRefreshToken = Request.Cookies["refresh_token"];
            var refreshToken = !string.IsNullOrWhiteSpace(cookieRefreshToken) ? cookieRefreshToken : req.RefreshToken;

            var result = await _auth.RefreshTokenAsync(
                refreshToken,
                req.DeviceId,
                req.UserAgent ?? Request.Headers["User-Agent"],
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            // Rotate refresh token cookie
            if (!string.IsNullOrWhiteSpace(result.refreshToken))
            {
                Response.Cookies.Append(
                    "refresh_token",
                    result.refreshToken,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.None,
                        Path = "/",
                        Expires = result.refreshExpires
                    }
                );
                // Thêm cookie flag cho client
                Response.Cookies.Append(
                    "logged_in",
                    "true",
                    new CookieOptions
                    {
                        HttpOnly = false, // để client có thể đọc được
                        Secure = true,
                        SameSite = SameSiteMode.None,
                        Path = "/",
                        Expires = result.refreshExpires
                    }
                );
            }

            // Do not return refresh token in body
            return Ok(new { accessToken = result.accessToken, accessTokenExpiresAt = result.accessExpires });
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
        var sub = User?.Claims
                      ?.FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
                      ?.Value
                  ?? User?.Claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)
                      ?.Value;
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

    /// <summary>
    /// Logout từ thiết bị hiện tại (giữ đăng nhập trên thiết bị khác)
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutDeviceRequest? req = null)
    {
        var sub = User?.Claims
                      ?.FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
                      ?.Value
                  ?? User?.Claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)
                      ?.Value;
        if (sub == null)
        {
            // Clear cookie anyway
            Response.Cookies.Delete("refresh_token", new CookieOptions { Path = "/" });
            return Ok();
        }

        // Nếu có deviceId trong request, chỉ logout device đó
        // Nếu không có, logout tất cả (backward compatibility)
        if (req?.DeviceId != null)
        {
            await _auth.LogoutDeviceAsync(Guid.Parse(sub), req.DeviceId);
        }
        else
        {
            await _auth.LogoutAllDevicesAsync(Guid.Parse(sub));
        }

        // Clear refresh token cookie
        Response.Cookies.Delete("refresh_token", new CookieOptions { Path = "/" });
        Response.Cookies.Delete("logged_in", new CookieOptions { Path = "/" });

        return Ok();
    }
}
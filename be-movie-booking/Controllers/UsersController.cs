using be_movie_booking.DTOs;
using be_movie_booking.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace be_movie_booking.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;
    private readonly IDistributedCache _cache;

    public UsersController(IUserService users, IDistributedCache cache)
    {
        _users = users;
        _cache = cache;
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        // Lấy userId từ claim
        var sub = User?.Claims
                      ?.FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
                      ?.Value
                  ?? User?.Claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)
                      ?.Value;

        if (sub == null) return Unauthorized();

        string cacheKey = $"user_me_{sub}";
        var cachedUserBytes = await _cache.GetAsync(cacheKey);

        if (cachedUserBytes != null)
        {
            // Deserialize từ JSON
            var cachedUser = System.Text.Json.JsonSerializer.Deserialize<UserReadDto>(cachedUserBytes);
            Console.WriteLine("Retrieved user from cache.");
            return Ok(cachedUser);
        }

        var dto = await _users.GetMeAsync(Guid.Parse(sub));

        if (dto == null) return NotFound();

        // Serialize và set cache với TTL 15 phút
        var dtoBytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(dto);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };
        if (dtoBytes != null)
        {
            await _cache.SetAsync(cacheKey, dtoBytes, options);
            Console.WriteLine("Stored user in cache.");
        }

        return dto == null ? NotFound() : Ok(dto);
    }

    [Authorize]
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UserUpdateMeDto req)
    {
        var sub = User?.Claims
                      ?.FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
                      ?.Value
                  ?? User?.Claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)
                      ?.Value;
        if (sub == null) return Unauthorized();

        var dto = await _users.UpdateMeAsync(Guid.Parse(sub), req);
        // Remove cached user info after update to avoid stale data
        string cacheKey = $"user_me_{sub}";
        await _cache.RemoveAsync(cacheKey);
        
        return dto == null ? NotFound() : Ok(dto);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var result = await _users.ListUsersAsync(page, pageSize, search);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var dto = await _users.GetByIdAsync(id);
        return dto == null ? NotFound() : Ok(dto);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id}/assign-admin")]
    public async Task<IActionResult> AssignAdminRole([FromRoute] Guid id)
    {
        var result = await _users.AssignAdminRoleAsync(id);
        return result ? Ok() : NotFound();
    }
}
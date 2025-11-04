using be_movie_booking.DTOs;
using be_movie_booking.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace be_movie_booking.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;
    private readonly IDatabase _db;

    public UsersController(IUserService users, IConnectionMultiplexer redis)
    {
        _users = users;
        _db = redis.GetDatabase();
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
        // Lấy cache từ Redis
        var cachedUserBytes = await _db.StringGetAsync(cacheKey);

        if (cachedUserBytes.HasValue)
        {
            // Deserialize từ JSON
            var cachedUser = System.Text.Json.JsonSerializer.Deserialize<UserReadDto>(cachedUserBytes);
            Console.WriteLine($"Retrived user from Redis cache with id {sub}.");
            return Ok(cachedUser);
        }

        var dto = await _users.GetMeAsync(Guid.Parse(sub));

        if (dto == null) return NotFound();

        // Lưu cache vào Redis với TTL 5 phút
        var dtoBytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(dto);
        await _db.StringSetAsync(cacheKey, dtoBytes, TimeSpan.FromMinutes(5));
        Console.WriteLine("Stored user in Redis cache.");

        return Ok(dto);
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
        // Xóa cache sau khi update
        string cacheKey = $"user_me_{sub}";
        await _db.KeyDeleteAsync(cacheKey);
        Console.WriteLine("Deleted user in Redis cache.");

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
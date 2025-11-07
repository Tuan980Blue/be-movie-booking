using be_movie_booking.Services;
using be_movie_booking.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace be_movie_booking.Controllers;

/// <summary>
/// Controller quản lý khóa/mở khóa ghế theo suất chiếu
/// </summary>
[ApiController]
[Route("api/seat-locks")]
public class SeatLocksController : ControllerBase
{
    private readonly ISeatLockService _seatLockService;

    public SeatLocksController(ISeatLockService seatLockService)
    {
        _seatLockService = seatLockService;
    }

    /// <summary>
    /// Khóa ghế ngồi
    /// </summary>
    [Authorize]
    [HttpPost("lock")]
    public async Task<IActionResult> LockSeats([FromBody] SeatLockRequestDto requestDto)
    {
        //lấy userId từ claim nếu có
        var sub = User?.Claims
                      ?.FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
                      ?.Value
                  ?? User?.Claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)
                      ?.Value;
        if (sub == null) return Unauthorized();

        requestDto.UserId = Guid.Parse(sub);
        
        try
        {
            var result = await _seatLockService.LockSeatsAsync(requestDto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    /// <summary>
    /// Thay đổi thời gian khóa ghế khi người dùng đang trong quá trình thanh toán
    /// </summary>
    [Authorize]
    [HttpPost("change-ttl")]
    public async Task<IActionResult> ChangeTimeLockSeats([FromBody] SeatLockExtendRequestDto requestDto)
    {
        //lấy userId từ claim nếu có
        var sub = User?.Claims
                      ?.FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
                      ?.Value
                  ?? User?.Claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)
                      ?.Value;
        if (sub == null) return Unauthorized(); 
        requestDto.UserId =  Guid.Parse(sub);
        try
        {
            var result = await _seatLockService.ChangeTimeLockSeatsAsync(requestDto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    
    /// <summary>
    /// Mở khóa ghế ngồi
    /// </summary>
    //[Authorize]
    [HttpPost("unlock")]
    public async Task<IActionResult> UnlockSeats([FromBody] SeatUnlockRequestDto requestDto)
    {
        var sub = User?.Claims
                      ?.FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
                      ?.Value
                  ?? User?.Claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)
                      ?.Value;
        if (sub == null) return Unauthorized();
        requestDto.UserId = Guid.Parse(sub);
        try
        {
            var result = await _seatLockService.UnlockSeatsAsync(requestDto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy danh sách ghế đang bị khóa của một suất chiếu
    /// </summary>
    [HttpGet("showtime/{showtimeId}")]
    public async Task<IActionResult> GetLockedSeats([FromRoute] Guid showtimeId)
    {
        try
        {
            var result = await _seatLockService.GetLockedSeatsAsync(showtimeId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
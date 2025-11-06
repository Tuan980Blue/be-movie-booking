using be_movie_booking.DTOs;
using be_movie_booking.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace be_movie_booking.Controllers;

/// <summary>
/// Controller để quản lý booking
/// </summary>
[ApiController]
[Route("api/bookings")]
public class BookingController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    /// <summary>
    /// Tạo booking mới
    /// </summary>
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBookingDto dto)
    {
        // Lấy userId từ claim
        var sub = User?.Claims
                      ?.FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
                      ?.Value
                  ?? User?.Claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)
                      ?.Value;
        if (sub == null) return Unauthorized();
        var userId = Guid.Parse(sub);
        try
        {
            //Kiểm tra dữ liệu đầu vào
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var booking = await _bookingService.CreateAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { id = booking.Id }, booking);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Có lỗi xảy ra khi tạo booking" });
        }
    }

    /// <summary>
    /// Lấy chi tiết booking theo ID
    /// </summary>
    [Authorize]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        try
        {
            var booking = await _bookingService.GetByIdAsync(id);
            if (booking == null)
            {
                return NotFound();
            }

            // Check if user owns this booking or is admin
            var sub = User?.Claims
                          ?.FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
                          ?.Value
                      ?? User?.Claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)
                          ?.Value;
            var userId = sub != null ? Guid.Parse(sub) : Guid.Empty;
            var isAdmin = User?.IsInRole("Admin") ?? false;

            if (booking.UserId != userId && !isAdmin)
            {
                return Forbid();
            }

            return Ok(booking);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Có lỗi xảy ra khi lấy thông tin booking" });
        }
    }

    /// <summary>
    /// Lấy chi tiết booking theo mã booking
    /// </summary>
    [Authorize]
    [HttpGet("code/{code}")]
    public async Task<IActionResult> GetByCode([FromRoute] string code)
    {
        try
        {
            var booking = await _bookingService.GetByCodeAsync(code);
            if (booking == null)
            {
                return NotFound();
            }

            // Check if user owns this booking or is admin
            var sub = User?.Claims
                          ?.FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
                          ?.Value
                      ?? User?.Claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)
                          ?.Value;
            var userId = sub != null ? Guid.Parse(sub) : Guid.Empty;
            var isAdmin = User?.IsInRole("Admin") ?? false;

            if (booking.UserId != userId && !isAdmin)
            {
                return Forbid();
            }

            return Ok(booking);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Có lỗi xảy ra khi lấy thông tin booking" });
        }
    }

    /// <summary>
    /// Lấy danh sách bookings với phân trang
    /// </summary>
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] BookingSearchDto searchDto)
    {
        try
        {
            // If not admin, only show user's own bookings
            var sub = User?.Claims
                          ?.FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
                          ?.Value
                      ?? User?.Claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)
                          ?.Value;
            var userId = sub != null ? Guid.Parse(sub) : Guid.Empty;
            var isAdmin = User?.IsInRole("Admin") ?? false;

            if (!isAdmin)
            {
                searchDto.UserId = userId;
            }

            var result = await _bookingService.ListAsync(searchDto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Có lỗi xảy ra khi lấy danh sách booking" });
        }
    }

    /// <summary>
    /// Xác nhận booking (sau khi thanh toán thành công)
    /// </summary>
    [Authorize]
    [HttpPost("{id}/confirm")]
    public async Task<IActionResult> Confirm([FromRoute] Guid id, [FromBody] ConfirmBookingDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var booking = await _bookingService.ConfirmAsync(id, dto.PaymentId);
            if (booking == null)
            {
                return NotFound();
            }

            // Check if user owns this booking or is admin
            var sub = User?.Claims
                          ?.FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
                          ?.Value
                      ?? User?.Claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)
                          ?.Value;
            var userId = sub != null ? Guid.Parse(sub) : Guid.Empty;
            var isAdmin = User?.IsInRole("Admin") ?? false;

            if (booking.UserId != userId && !isAdmin)
            {
                return Forbid();
            }

            return Ok(booking);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Có lỗi xảy ra khi xác nhận booking" });
        }
    }

    /// <summary>
    /// Hủy booking
    /// </summary>
    [Authorize]
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel([FromRoute] Guid id, [FromBody] CancelBookingDto? dto = null)
    {
        try
        {
            var booking = await _bookingService.CancelAsync(id, dto?.Reason);
            if (booking == null)
            {
                return NotFound();
            }

            // Check if user owns this booking or is admin
            var sub = User?.Claims
                          ?.FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
                          ?.Value
                      ?? User?.Claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)
                          ?.Value;
            var userId = sub != null ? Guid.Parse(sub) : Guid.Empty;
            var isAdmin = User?.IsInRole("Admin") ?? false;

            if (booking.UserId != userId && !isAdmin)
            {
                return Forbid();
            }

            return Ok(booking);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Có lỗi xảy ra khi hủy booking" });
        }
    }
}

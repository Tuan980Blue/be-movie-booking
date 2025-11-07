using be_movie_booking.DTOs;
using be_movie_booking.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace be_movie_booking.Controllers;

/// <summary>
/// Controller để quản lý ticket check-in (dành cho nhân viên rạp chiếu phim)
/// </summary>
[Authorize(Roles = ("Manager,Admin"))]
[ApiController]
[Route("api/tickets")]
public class TicketController : ControllerBase
{
    private readonly ITicketService _ticketService;

    public TicketController(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    /// <summary>
    /// Verify QR code (BookingCode) và xem thông tin booking với tất cả tickets (chỉ xem, không check-in)
    /// </summary>
    [Authorize(Roles = "Manager,Admin")]
    [HttpGet("qr/{qrCode}")]
    public async Task<IActionResult> VerifyQrCode([FromRoute] string qrCode)
    {
        try
        {
            var result = await _ticketService.VerifyBookingByQrCodeAsync(qrCode);
            if (result == null)
            {
                return NotFound(new { message = "Không tìm thấy đơn hàng" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Có lỗi xảy ra khi verify QR code" });
        }
    }

    /// <summary>
    /// Check-in booking (check-in tất cả tickets trong booking)
    /// </summary>
    [Authorize(Roles = "Manager,Admin")]
    [HttpPost("{bookingCode}/checkin")]
    public async Task<IActionResult> CheckIn([FromRoute] string bookingCode)
    {
        try
        {
            // Lấy staffId từ claim
            var sub = User?.Claims
                          ?.FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
                          ?.Value
                      ?? User?.Claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)
                          ?.Value;
            if (sub == null) return Unauthorized();
            var staffId = Guid.Parse(sub);

            var result = await _ticketService.CheckInBookingAsync(bookingCode, staffId);
            if (result == null)
            {
                return NotFound(new { message = "Không tìm thấy đơn hàng" });
            }

            if (!result.IsValid)
            {
                return BadRequest(new { message = result.ValidationMessage });
            }

            if (result.IsFullyCheckedIn && result.ValidationMessage?.Contains("đã được check-in") == true)
            {
                return Conflict(new { message = result.ValidationMessage, booking = result });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Có lỗi xảy ra khi check-in đơn hàng" });
        }
    }
}


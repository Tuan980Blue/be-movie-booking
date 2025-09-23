using be_movie_booking.DTOs;
using be_movie_booking.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace be_movie_booking.Controllers;

/// <summary>
/// Controller để quản lý ghế ngồi
/// </summary>
[ApiController]
[Route("api/cinemas/{cinemaId}/rooms/{roomId}/seats")]
public class SeatsController : ControllerBase
{
    private readonly ISeatService _seatService;
    private readonly IRoomService _roomService;

    public SeatsController(ISeatService seatService, IRoomService roomService)
    {
        _seatService = seatService;
        _roomService = roomService;
    }

    /// <summary>
    /// Lấy danh sách ghế ngồi của phòng với phân trang và tìm kiếm
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List([FromRoute] Guid cinemaId, [FromRoute] Guid roomId, [FromQuery] SeatSearchDto searchDto)
    {
        try
        {
            var room = await _roomService.GetByIdAsync(roomId);
            if (room == null || room.CinemaId != cinemaId) return NotFound(new { message = "Room not found in this cinema" });
            var result = await _seatService.ListByRoomAsync(roomId, searchDto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy layout ghế ngồi của phòng (cho frontend hiển thị)
    /// </summary>
    [HttpGet("layout")]
    public async Task<IActionResult> GetLayout([FromRoute] Guid cinemaId, [FromRoute] Guid roomId)
    {
        try
        {
            var room = await _roomService.GetByIdAsync(roomId);
            if (room == null || room.CinemaId != cinemaId) return NotFound(new { message = "Room not found in this cinema" });
            var layout = await _seatService.GetSeatLayoutAsync(roomId);
            return Ok(layout);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy chi tiết ghế ngồi theo ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        try
        {
            var seat = await _seatService.GetByIdAsync(id);
            return seat == null ? NotFound() : Ok(seat);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy danh sách ghế có sẵn trong phòng
    /// </summary>
    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableSeats([FromRoute] Guid cinemaId, [FromRoute] Guid roomId)
    {
        try
        {
            var room = await _roomService.GetByIdAsync(roomId);
            if (room == null || room.CinemaId != cinemaId) return NotFound(new { message = "Room not found in this cinema" });
            var seats = await _seatService.GetAvailableSeatsAsync(roomId);
            return Ok(seats);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy danh sách ghế theo loại
    /// </summary>
    [HttpGet("by-type/{seatType}")]
    public async Task<IActionResult> GetSeatsByType([FromRoute] Guid cinemaId, [FromRoute] Guid roomId, [FromRoute] string seatType)
    {
        try
        {
            var room = await _roomService.GetByIdAsync(roomId);
            if (room == null || room.CinemaId != cinemaId) return NotFound(new { message = "Room not found in this cinema" });
            var seats = await _seatService.GetSeatsByTypeAsync(roomId, seatType);
            return Ok(seats);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy thống kê ghế ngồi của phòng
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats([FromRoute] Guid cinemaId, [FromRoute] Guid roomId)
    {
        try
        {
            var room = await _roomService.GetByIdAsync(roomId);
            if (room == null || room.CinemaId != cinemaId) return NotFound(new { message = "Room not found in this cinema" });
            var stats = await _seatService.GetStatsAsync(roomId);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Tạo ghế ngồi mới (Admin/Manager only)
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    public async Task<IActionResult> Create([FromRoute] Guid cinemaId, [FromRoute] Guid roomId, [FromBody] CreateSeatDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var room = await _roomService.GetByIdAsync(roomId);
            if (room == null || room.CinemaId != cinemaId) return NotFound(new { message = "Room not found in this cinema" });
            var seat = await _seatService.CreateAsync(roomId, dto);
            return seat == null ? BadRequest() : CreatedAtAction(nameof(GetById), new { cinemaId, roomId, id = seat.Id }, seat);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Có lỗi xảy ra khi tạo ghế ngồi" });
        }
    }

    /// <summary>
    /// Tạo layout ghế hàng loạt (Admin/Manager only)
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
    [HttpPost("bulk-layout")]
    public async Task<IActionResult> CreateBulkLayout([FromRoute] Guid cinemaId, [FromRoute] Guid roomId, [FromBody] CreateSeatLayoutDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var room = await _roomService.GetByIdAsync(roomId);
            if (room == null || room.CinemaId != cinemaId) return NotFound(new { message = "Room not found in this cinema" });
            var seats = await _seatService.CreateBulkLayoutAsync(roomId, dto);
            return Ok(new { message = $"Đã tạo {seats.Count} ghế ngồi", seats });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Có lỗi xảy ra khi tạo layout ghế" });
        }
    }

    /// <summary>
    /// Cập nhật thông tin ghế ngồi (Admin/Manager only)
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update([FromRoute] Guid cinemaId, [FromRoute] Guid roomId, [FromRoute] Guid id, [FromBody] UpdateSeatDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var room = await _roomService.GetByIdAsync(roomId);
            if (room == null || room.CinemaId != cinemaId) return NotFound(new { message = "Room not found in this cinema" });
            var seat = await _seatService.UpdateAsync(id, dto);
            return seat == null ? NotFound() : Ok(seat);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Có lỗi xảy ra khi cập nhật ghế ngồi" });
        }
    }

    

    /// <summary>
    /// Xóa ghế ngồi (Admin only)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete([FromRoute] Guid cinemaId, [FromRoute] Guid roomId, [FromRoute] Guid id)
    {
        try
        {
            var room = await _roomService.GetByIdAsync(roomId);
            if (room == null || room.CinemaId != cinemaId) return NotFound(new { message = "Room not found in this cinema" });
            var result = await _seatService.DeleteAsync(id);
            return result ? NoContent() : NotFound();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Có lỗi xảy ra khi xóa ghế ngồi" });
        }
    }

    /// <summary>
    /// Xóa tất cả ghế ngồi trong phòng (Admin only)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpDelete("all")]
    public async Task<IActionResult> DeleteAll([FromRoute] Guid cinemaId, [FromRoute] Guid roomId)
    {
        try
        {
            var room = await _roomService.GetByIdAsync(roomId);
            if (room == null || room.CinemaId != cinemaId) return NotFound(new { message = "Room not found in this cinema" });
            var result = await _seatService.DeleteByRoomAsync(roomId);
            return result ? NoContent() : NotFound();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Có lỗi xảy ra khi xóa tất cả ghế ngồi" });
        }
    }
}

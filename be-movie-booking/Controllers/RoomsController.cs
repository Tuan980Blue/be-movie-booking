using be_movie_booking.DTOs;
using be_movie_booking.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace be_movie_booking.Controllers;

/// <summary>
/// Controller để quản lý phòng chiếu
/// </summary>
[ApiController]
[Route("api/cinemas/{cinemaId}/rooms")]
public class RoomsController : ControllerBase
{
    private readonly IRoomService _roomService;

    public RoomsController(IRoomService roomService)
    {
        _roomService = roomService;
    }

    /// <summary>
    /// Lấy danh sách phòng chiếu của rạp với phân trang và tìm kiếm
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List([FromRoute] Guid cinemaId, [FromQuery] RoomSearchDto searchDto)
    {
        try
        {
            var result = await _roomService.ListByCinemaAsync(cinemaId, searchDto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy chi tiết phòng chiếu theo ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        try
        {
            var room = await _roomService.GetByIdAsync(id);
            return room == null ? NotFound() : Ok(room);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Tạo phòng chiếu mới (Admin/Manager only)
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    public async Task<IActionResult> Create([FromRoute] Guid cinemaId, [FromBody] CreateRoomDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var room = await _roomService.CreateAsync(cinemaId, dto);
            return room == null ? BadRequest() : CreatedAtAction(nameof(GetById), new { cinemaId, id = room.Id }, room);
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
            return StatusCode(500, new { message = "Có lỗi xảy ra khi tạo phòng chiếu" });
        }
    }

    /// <summary>
    /// Cập nhật thông tin phòng chiếu (Admin/Manager only)
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateRoomDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var room = await _roomService.UpdateAsync(id, dto);
            return room == null ? NotFound() : Ok(room);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Có lỗi xảy ra khi cập nhật phòng chiếu" });
        }
    }

    /// <summary>
    /// Xóa phòng chiếu (Admin only)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        try
        {
            var result = await _roomService.DeleteAsync(id);
            return result ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Có lỗi xảy ra khi xóa phòng chiếu" });
        }
    }

    /// <summary>
    /// Thay đổi trạng thái phòng chiếu (Admin/Manager only)
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> ChangeStatus([FromRoute] Guid id, [FromBody] ChangeRoomStatusDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var room = await _roomService.ChangeStatusAsync(id, dto);
            return room == null ? NotFound() : Ok(room);
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
            return StatusCode(500, new { message = "Có lỗi xảy ra khi thay đổi trạng thái phòng chiếu" });
        }
    }

    /// <summary>
    /// Lấy thống kê phòng chiếu (Admin/Manager only)
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
    [HttpGet("{id}/stats")]
    public async Task<IActionResult> GetStats([FromRoute] Guid id)
    {
        try
        {
            var stats = await _roomService.GetStatsAsync(id);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Có lỗi xảy ra khi lấy thống kê" });
        }
    }
}

using be_movie_booking.DTOs;
using be_movie_booking.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace be_movie_booking.Controllers;

/// <summary>
/// Controller để quản lý rạp chiếu phim
/// </summary>
[ApiController]
[Route("api/cinemas")]
public class CinemasController : ControllerBase
{
    private readonly ICinemaService _cinemaService;

    public CinemasController(ICinemaService cinemaService)
    {
        _cinemaService = cinemaService;
    }

    /// <summary>
    /// Lấy danh sách rạp chiếu phim với phân trang và tìm kiếm
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] CinemaSearchDto searchDto)
    {
        try
        {
            var result = await _cinemaService.ListAsync(searchDto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy chi tiết rạp chiếu phim theo ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        try
        {
            var cinema = await _cinemaService.GetByIdAsync(id);
            return cinema == null ? NotFound() : Ok(cinema);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Tạo rạp chiếu phim mới (Admin/Manager only)
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCinemaDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var cinema = await _cinemaService.CreateAsync(dto);
            return cinema == null ? BadRequest() : CreatedAtAction(nameof(GetById), new { id = cinema.Id }, cinema);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Có lỗi xảy ra khi tạo rạp chiếu phim" });
        }
    }

    /// <summary>
    /// Cập nhật thông tin rạp chiếu phim (Admin/Manager only)
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateCinemaDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var cinema = await _cinemaService.UpdateAsync(id, dto);
            return cinema == null ? NotFound() : Ok(cinema);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Có lỗi xảy ra khi cập nhật rạp chiếu phim" });
        }
    }

    /// <summary>
    /// Xóa rạp chiếu phim (Admin only)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        try
        {
            var result = await _cinemaService.DeleteAsync(id);
            return result ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Có lỗi xảy ra khi xóa rạp chiếu phim" });
        }
    }

    /// <summary>
    /// Thay đổi trạng thái rạp chiếu phim (Admin/Manager only)
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> ChangeStatus([FromRoute] Guid id, [FromBody] ChangeCinemaStatusDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var cinema = await _cinemaService.ChangeStatusAsync(id, dto);
            return cinema == null ? NotFound() : Ok(cinema);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Có lỗi xảy ra khi thay đổi trạng thái rạp chiếu phim" });
        }
    }

    /// <summary>
    /// Lấy thống kê rạp chiếu phim (Admin/Manager only)
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
    [HttpGet("{id}/stats")]
    public async Task<IActionResult> GetStats([FromRoute] Guid id)
    {
        try
        {
            var stats = await _cinemaService.GetStatsAsync(id);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Có lỗi xảy ra khi lấy thống kê" });
        }
    }
}

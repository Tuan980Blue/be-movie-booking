using be_movie_booking.DTOs;
using be_movie_booking.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace be_movie_booking.Controllers;

/// <summary>
/// Controller để quản lý suất chiếu - CHỈ CÁC CHỨC NĂNG CỐT LÕI
/// </summary>
[ApiController]
[Route("api/showtimes")]
public class ShowtimesController : ControllerBase
{
    private readonly IShowtimeService _showtimeService;

    public ShowtimesController(IShowtimeService showtimeService)
    {
        _showtimeService = showtimeService;
    }

    /// <summary>
    /// Lấy danh sách suất chiếu với filter cơ bản
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] ShowtimeSearchDto searchDto)
    {
        try
        {
            var result = await _showtimeService.ListAsync(searchDto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy thông tin chi tiết suất chiếu theo ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        try
        {
            var showtime = await _showtimeService.GetByIdAsync(id);
            return showtime == null ? NotFound() : Ok(showtime);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy danh sách suất chiếu theo ID phim
    /// </summary>
    [HttpGet("movie/{movieId}")]
    public async Task<IActionResult> GetByMovieId([FromRoute] Guid movieId)
    {
        try
        {
            var showtimes = await _showtimeService.ListByMovieIdAsync(movieId);
            return Ok(showtimes);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Tạo suất chiếu mới (Admin/Manager only)
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateShowtimeDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var showtime = await _showtimeService.CreateAsync(dto);
            return showtime == null ? BadRequest() : CreatedAtAction(nameof(GetByMovieId), new { movieId = showtime.MovieId }, showtime);
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
            return StatusCode(500, new { message = "Có lỗi xảy ra khi tạo suất chiếu" });
        }
    }

    /// <summary>
    /// Cập nhật suất chiếu (Admin/Manager only)
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateShowtimeDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var showtime = await _showtimeService.UpdateAsync(id, dto);
            return showtime == null ? NotFound() : Ok(showtime);
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
            return StatusCode(500, new { message = "Có lỗi xảy ra khi cập nhật suất chiếu" });
        }
    }

    /// <summary>
    /// Xóa suất chiếu (Admin only)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        try
        {
            var result = await _showtimeService.DeleteAsync(id);
            return result ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Có lỗi xảy ra khi xóa suất chiếu" });
        }
    }
}

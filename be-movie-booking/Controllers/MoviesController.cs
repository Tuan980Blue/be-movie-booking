using be_movie_booking.DTOs;
using be_movie_booking.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using be_movie_booking.Hubs;

namespace be_movie_booking.Controllers;

/// <summary>
/// Controller để quản lý phim
/// </summary>
[ApiController]
[Route("api/movies")]
public class MoviesController : ControllerBase
{
    private readonly IMovieService _movieService;
    private readonly IHubContext<AppHub> _hubContext;

    public MoviesController(IMovieService movieService, IHubContext<AppHub> hubContext)
    {
        _movieService = movieService;
        _hubContext = hubContext;
    }

    /// <summary>
    /// Lấy danh sách phim với phân trang và tìm kiếm
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] MovieSearchDto searchDto)
    {
        try
        {
            var result = await _movieService.ListAsync(searchDto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy chi tiết phim theo ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        try
        {
            var movie = await _movieService.GetByIdAsync(id);
            return movie == null ? NotFound() : Ok(movie);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Tạo phim mới (Admin/Manager only)
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMovieDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var movie = await _movieService.CreateAsync(dto);
            if (movie != null)
            {
                // Gửi thông báo real-time 
                await _hubContext.Clients.Group("movies").SendAsync("movies_updated");
            }
            return movie == null ? BadRequest() : CreatedAtAction(nameof(GetById), new { id = movie.Id }, movie);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Có lỗi xảy ra khi tạo phim" });
        }
    }

    /// <summary>
    /// Cập nhật phim (Admin/Manager only)
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateMovieDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var movie = await _movieService.UpdateAsync(id, dto);
            if (movie != null)
            {
                // Gửi thông báo real-time về việc movies đã được cập nhật
                await _hubContext.Clients.Group("movies").SendAsync("movies_updated");
            }
            return movie == null ? NotFound() : Ok(movie);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Có lỗi xảy ra khi cập nhật phim" });
        }
    }

    /// <summary>
    /// Xóa phim (Admin only)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        try
        {
            var result = await _movieService.DeleteAsync(id);
            if (result)
            {
                // Gửi thông báo real-time về việc movies đã được cập nhật
                await _hubContext.Clients.Group("movies").SendAsync("movies_updated");
            }
            return result ? NoContent() : NotFound();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Có lỗi xảy ra khi xóa phim" });
        }
    }

    /// <summary>
    /// Thay đổi trạng thái phim (Admin/Manager only)
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> ChangeStatus([FromRoute] Guid id, [FromBody] ChangeMovieStatusDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var movie = await _movieService.ChangeStatusAsync(id, dto);
            if (movie != null)
            {
                // Gửi thông báo real-time về việc movies đã được cập nhật
                await _hubContext.Clients.Group("movies").SendAsync("movies_updated");
            }
            return movie == null ? NotFound() : Ok(movie);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Có lỗi xảy ra khi thay đổi trạng thái phim" });
        }
    }

    /// <summary>
    /// Lấy danh sách phim theo trạng thái
    /// </summary>
    [HttpGet("status/{status}")]
    public async Task<IActionResult> GetByStatus([FromRoute] string status)
    {
        try
        {
            var movies = await _movieService.GetByStatusAsync(status);
            return Ok(movies);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Có lỗi xảy ra khi lấy danh sách phim" });
        }
    }

    /// <summary>
    /// Lấy thống kê phim (Admin only)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            var stats = await _movieService.GetStatsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Có lỗi xảy ra khi lấy thống kê" });
        }
    }
}

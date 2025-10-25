using be_movie_booking.DTOs;
using be_movie_booking.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using be_movie_booking.Hubs;

namespace be_movie_booking.Controllers;

/// <summary>
/// Controller để quản lý thể loại phim
/// </summary>
[ApiController]
[Route("api/genres")]
public class GenresController : ControllerBase
{
    private readonly IGenreService _genreService;
    private readonly IHubContext<AppHub> _hubContext;

    public GenresController(IGenreService genreService, IHubContext<AppHub> hubContext)
    {
        _genreService = genreService;
        _hubContext = hubContext;
    }

    /// <summary>
    /// Lấy danh sách tất cả thể loại
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List()
    {
        try
        {
            var genres = await _genreService.ListAsync();
            return Ok(genres);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy chi tiết thể loại theo ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        try
        {
            var genre = await _genreService.GetByIdAsync(id);
            return genre == null ? NotFound() : Ok(genre);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Tạo thể loại mới (Admin only)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGenreDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var genre = await _genreService.CreateAsync(dto);
            if (genre != null)
            {
                //gửi thông báo real-time về việc genres đã được cập nhật(chỉ gửi group)
                await _hubContext.Clients.Group("genres").SendAsync("genres_updated");
            }
            return genre == null ? BadRequest() : CreatedAtAction(nameof(GetById), new { id = genre.Id }, genre);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Có lỗi xảy ra khi tạo thể loại" });
        }
    }

    /// <summary>
    /// Cập nhật thể loại (Admin only)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateGenreDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var genre = await _genreService.UpdateAsync(id, dto);
            if (genre != null)
            {
                // Gửi thông báo real-time về việc genres đã được cập nhật
                await _hubContext.Clients.Group("genres").SendAsync("genres_updated");
            }
            return genre == null ? NotFound() : Ok(genre);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Có lỗi xảy ra khi cập nhật thể loại" });
        }
    }

    /// <summary>
    /// Xóa thể loại (Admin only)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        try
        {
            var result = await _genreService.DeleteAsync(id);
            if (result)
            {
                // Gửi thông báo real-time về việc genres đã được cập nhật
                await _hubContext.Clients.Group("genres").SendAsync("genres_updated");
            }
            return result ? NoContent() : NotFound();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Có lỗi xảy ra khi xóa thể loại" });
        }
    }
}

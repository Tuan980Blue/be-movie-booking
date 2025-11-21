using be_movie_booking.DTOs;
using be_movie_booking.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using be_movie_booking.Hubs;
using StackExchange.Redis;
using System.Text.Json;

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
    private readonly IDatabase _redis;

    public MoviesController(IMovieService movieService, IHubContext<AppHub> hubContext, IConnectionMultiplexer redis)
    {
        _movieService = movieService;
        _hubContext = hubContext;
        _redis = redis.GetDatabase();
    }

    /// <summary>
    /// Tạo cache key từ MovieSearchDto
    /// </summary>
    private static string GetCacheKey(MovieSearchDto searchDto)
    {
        var parts = new List<string> { "movies:list" };
        
        if (!string.IsNullOrWhiteSpace(searchDto.Search))
            parts.Add($"search:{searchDto.Search.Trim().ToLower()}");
        
        if (searchDto.GenreIds != null && searchDto.GenreIds.Any())
            parts.Add($"genres:{string.Join(",", searchDto.GenreIds.OrderBy(g => g))}");
        
        if (searchDto.Status.HasValue)
            parts.Add($"status:{searchDto.Status.Value}");
        
        if (searchDto.ReleaseYear.HasValue)
            parts.Add($"year:{searchDto.ReleaseYear.Value}");
        
        if (searchDto.ReleaseDateFrom.HasValue)
            parts.Add($"from:{searchDto.ReleaseDateFrom.Value:yyyy-MM-dd}");
        
        if (searchDto.ReleaseDateTo.HasValue)
            parts.Add($"to:{searchDto.ReleaseDateTo.Value:yyyy-MM-dd}");
        
        parts.Add($"page:{searchDto.Page}");
        parts.Add($"pageSize:{searchDto.PageSize}");
        parts.Add($"sortBy:{searchDto.SortBy}");
        parts.Add($"sortDir:{searchDto.SortDirection}");
        
        return string.Join(":", parts);
    }

    /// <summary>
    /// Xóa tất cả cache liên quan đến movies list
    /// </summary>
    private async Task InvalidateMoviesCacheAsync()
    {
        try
        {
            var server = _redis.Multiplexer.GetServer(_redis.Multiplexer.GetEndPoints().First());
            
            // Xóa tất cả keys bắt đầu bằng "movies:list"
            await foreach (var key in server.KeysAsync(pattern: "movies:list:*"))
            {
                await _redis.KeyDeleteAsync(key);
            }
            
            // Xóa cache của GetByStatus
            await foreach (var key in server.KeysAsync(pattern: "movies:status:*"))
            {
                await _redis.KeyDeleteAsync(key);
            }
            
            Console.WriteLine("Invalidated movies cache.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error invalidating movies cache: {ex.Message}");
        }
    }

    /// <summary>
    /// Lấy danh sách phim với phân trang và tìm kiếm
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] MovieSearchDto searchDto)
    {
        try
        {
            // Tạo cache key từ search parameters
            var cacheKey = GetCacheKey(searchDto);
            
            // Kiểm tra cache trong Redis
            var cachedResult = await _redis.StringGetAsync(cacheKey);
            if (cachedResult.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<PagedResult<MovieReadDto>>(cachedResult);
                if (cachedData != null)
                {
                    Console.WriteLine($"Retrieved movies list from Redis cache with key: {cacheKey}");
                    return Ok(cachedData);
                }
            }

            // Nếu không có cache, lấy từ database
            var result = await _movieService.ListAsync(searchDto);
            
            // Lưu vào cache với TTL 10 phút
            var resultBytes = JsonSerializer.SerializeToUtf8Bytes(result);
            await _redis.StringSetAsync(cacheKey, resultBytes, TimeSpan.FromMinutes(10));
            Console.WriteLine($"Stored movies list in Redis cache with key: {cacheKey}");
            
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
            var cacheKey = $"movies:detail:{id}";
            
            // Kiểm tra cache
            var cachedMovie = await _redis.StringGetAsync(cacheKey);
            if (cachedMovie.HasValue)
            {
                var movie = JsonSerializer.Deserialize<MovieReadDto>(cachedMovie);
                if (movie != null)
                {
                    Console.WriteLine($"Retrieved movie detail from Redis cache with id: {id}");
                    return Ok(movie);
                }
            }

            // Lấy từ database
            var result = await _movieService.GetByIdAsync(id);
            if (result == null) return NotFound();

            // Lưu vào cache với TTL 15 phút
            var resultBytes = JsonSerializer.SerializeToUtf8Bytes(result);
            await _redis.StringSetAsync(cacheKey, resultBytes, TimeSpan.FromMinutes(15));
            Console.WriteLine($"Stored movie detail in Redis cache with id: {id}");

            return Ok(result);
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
                // Xóa cache khi có thay đổi
                await InvalidateMoviesCacheAsync();
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
                // Xóa cache khi có thay đổi
                await InvalidateMoviesCacheAsync();
                // Xóa cache chi tiết phim
                await _redis.KeyDeleteAsync($"movies:detail:{id}");
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
                // Xóa cache khi có thay đổi
                await InvalidateMoviesCacheAsync();
                // Xóa cache chi tiết phim
                await _redis.KeyDeleteAsync($"movies:detail:{id}");
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
                // Xóa cache khi có thay đổi
                await InvalidateMoviesCacheAsync();
                // Xóa cache chi tiết phim
                await _redis.KeyDeleteAsync($"movies:detail:{id}");
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
            var cacheKey = $"movies:status:{status.ToLower()}";
            
            // Kiểm tra cache
            var cachedMovies = await _redis.StringGetAsync(cacheKey);
            if (cachedMovies.HasValue)
            {
                var movies = JsonSerializer.Deserialize<List<MovieReadDto>>(cachedMovies);
                if (movies != null)
                {
                    Console.WriteLine($"Retrieved movies by status from Redis cache: {status}");
                    return Ok(movies);
                }
            }

            // Lấy từ database
            var result = await _movieService.GetByStatusAsync(status);
            
            // Lưu vào cache với TTL 10 phút
            var resultBytes = JsonSerializer.SerializeToUtf8Bytes(result);
            await _redis.StringSetAsync(cacheKey, resultBytes, TimeSpan.FromMinutes(10));
            Console.WriteLine($"Stored movies by status in Redis cache: {status}");

            return Ok(result);
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

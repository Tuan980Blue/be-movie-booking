using be_movie_booking.DTOs;
using be_movie_booking.Services;
using be_movie_booking.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using be_movie_booking.Hubs;
using StackExchange.Redis;
using System.Text.Json;

namespace be_movie_booking.Controllers;

/// <summary>
/// Controller để quản lý suất chiếu - CHỈ CÁC CHỨC NĂNG CỐT LÕI
/// </summary>
[ApiController]
[Route("api/showtimes")]
public class ShowtimesController : ControllerBase
{
    private readonly IShowtimeService _showtimeService;
    private readonly IHubContext<AppHub> _hubContext;
    private readonly IBookingRepository _bookingRepository;
    private readonly IDatabase _redis;

    public ShowtimesController(IShowtimeService showtimeService, IHubContext<AppHub> hubContext, IBookingRepository bookingRepository, IConnectionMultiplexer redis)
    {
        _showtimeService = showtimeService;
        _hubContext = hubContext;
        _bookingRepository = bookingRepository;
        _redis = redis.GetDatabase();
    }

    /// <summary>
    /// Xóa cache showtimes theo movieId
    /// </summary>
    private async Task InvalidateShowtimesByMovieCacheAsync(Guid movieId)
    {
        try
        {
            var cacheKey = $"showtimes:movie:{movieId}";
            await _redis.KeyDeleteAsync(cacheKey);
            Console.WriteLine($"Invalidated showtimes cache for movie: {movieId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error invalidating showtimes cache: {ex.Message}");
        }
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
            var cacheKey = $"showtimes:movie:{movieId}";
            
            // Kiểm tra cache trong Redis
            var cachedResult = await _redis.StringGetAsync(cacheKey);
            if (cachedResult.HasValue)
            {
                var cachedShowtimes = JsonSerializer.Deserialize<List<ShowtimeReadDto>>(cachedResult);
                if (cachedShowtimes != null)
                {
                    Console.WriteLine($"Retrieved showtimes by movie from Redis cache with movieId: {movieId}");
                    return Ok(cachedShowtimes);
                }
            }

            // Nếu không có cache, lấy từ database
            var showtimes = await _showtimeService.ListByMovieIdAsync(movieId);
            
            // Lưu vào cache với TTL 10 phút
            var resultBytes = JsonSerializer.SerializeToUtf8Bytes(showtimes);
            await _redis.StringSetAsync(cacheKey, resultBytes, TimeSpan.FromMinutes(10));
            Console.WriteLine($"Stored showtimes by movie in Redis cache with movieId: {movieId}");
            
            return Ok(showtimes);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy danh sách ID ghế đã được đặt (Confirmed) theo suất chiếu
    /// </summary>
    [HttpGet("{id}/booked-seats")]
    public async Task<IActionResult> GetBookedSeats([FromRoute] Guid id)
    {
        try
        {
            // Validate showtime exists
            var showtime = await _showtimeService.GetByIdAsync(id);
            if (showtime == null)
            {
                return NotFound(new { message = "Suất chiếu không tồn tại" });
            }

            var cacheKey = $"showtimes:booked-seats:{id}";
            
            // Kiểm tra cache trong Redis
            var cachedResult = await _redis.StringGetAsync(cacheKey);
            if (cachedResult.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<List<Guid>>(cachedResult);
                if (cachedData != null)
                {
                    Console.WriteLine($"Retrieved booked seats from Redis cache with showtimeId: {id}");
                    return Ok(new
                    {
                        showtimeId = id,
                        bookedSeatIds = cachedData,
                        count = cachedData.Count
                    });
                }
            }

            // Nếu không có cache, lấy từ database
            var bookedSeatIds = await _bookingRepository.GetBookedSeatIdsAsync(id);

            // Lưu vào cache với TTL 2 phút (dữ liệu thay đổi thường xuyên khi có booking)
            var resultBytes = JsonSerializer.SerializeToUtf8Bytes(bookedSeatIds);
            await _redis.StringSetAsync(cacheKey, resultBytes, TimeSpan.FromMinutes(2));
            Console.WriteLine($"Stored booked seats in Redis cache with showtimeId: {id}");

            return Ok(new
            {
                showtimeId = id,
                bookedSeatIds = bookedSeatIds,
                count = bookedSeatIds.Count
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Có lỗi xảy ra khi lấy danh sách ghế đã đặt", error = ex.Message });
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
            if (showtime != null)
            {
                // Xóa cache khi có thay đổi
                await InvalidateShowtimesByMovieCacheAsync(showtime.MovieId);
                // Gửi thông báo real-time về việc showtimes đã được cập nhật
                await _hubContext.Clients.Group("showtimes").SendAsync("showtimes_updated", showtime.CinemaId, showtime.MovieId);
            }
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
            if (showtime != null)
            {
                // Xóa cache khi có thay đổi
                await InvalidateShowtimesByMovieCacheAsync(showtime.MovieId);
                // Gửi thông báo real-time về việc showtimes đã được cập nhật
                await _hubContext.Clients.Group("showtimes").SendAsync("showtimes_updated", showtime.CinemaId, showtime.MovieId);
            }
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
            // Lấy thông tin showtime trước khi xóa để biết movieId
            var showtimeBeforeDelete = await _showtimeService.GetByIdAsync(id);
            var movieId = showtimeBeforeDelete?.MovieId ?? Guid.Empty;
            
            var result = await _showtimeService.DeleteAsync(id);
            if (result)
            {
                // Xóa cache khi có thay đổi
                if (movieId != Guid.Empty)
                {
                    await InvalidateShowtimesByMovieCacheAsync(movieId);
                }
                // Gửi thông báo real-time về việc showtimes đã được cập nhật
                await _hubContext.Clients.Group("showtimes").SendAsync("showtimes_updated", 0, Guid.Empty);
            }
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

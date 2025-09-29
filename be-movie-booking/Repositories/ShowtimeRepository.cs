using be_movie_booking.Data;
using be_movie_booking.DTOs;
using be_movie_booking.Models;
using Microsoft.EntityFrameworkCore;

namespace be_movie_booking.Repositories;

/// <summary>
/// Interface cho Showtime Repository - CHỈ CÁC CHỨC NĂNG CỐT LÕI
/// </summary>
public interface IShowtimeRepository
{
    Task<Showtime?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Showtime?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<(List<Showtime> showtimes, int total)> ListAsync(ShowtimeSearchDto searchDto, CancellationToken ct = default);
    Task<List<Showtime>> ListByMovieIdAsync(Guid movieId, CancellationToken ct = default);
    Task<Showtime?> AddAsync(Showtime showtime, CancellationToken ct = default);
    Task<Showtime?> UpdateAsync(Showtime showtime, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
    Task<bool> HasTimeConflictAsync(Guid roomId, DateTime startUtc, DateTime endUtc, Guid? excludeId = null, CancellationToken ct = default);
    Task<int> GetAvailableSeatsCountAsync(Guid showtimeId, CancellationToken ct = default);
}

/// <summary>
/// Repository để xử lý data access cho Showtime
/// </summary>
public class ShowtimeRepository : IShowtimeRepository
{
    private readonly MovieBookingDbContext _db;

    public ShowtimeRepository(MovieBookingDbContext db)
    {
        _db = db;
    }

    public async Task<Showtime?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Showtimes
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<Showtime?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.Room)
            .ThenInclude(r => r.Cinema)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<(List<Showtime> showtimes, int total)> ListAsync(ShowtimeSearchDto searchDto, CancellationToken ct = default)
    {
        var query = _db.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.Room)
            .ThenInclude(r => r.Cinema)
            .AsQueryable();

        // Apply filters
        if (searchDto.MovieId.HasValue)
        {
            query = query.Where(s => s.MovieId == searchDto.MovieId.Value);
        }

        if (searchDto.CinemaId.HasValue)
        {
            query = query.Where(s => s.Room.CinemaId == searchDto.CinemaId.Value);
        }

        if (searchDto.RoomId.HasValue)
        {
            query = query.Where(s => s.RoomId == searchDto.RoomId.Value);
        }

        if (searchDto.DateFrom.HasValue)
        {
            query = query.Where(s => s.StartUtc >= searchDto.DateFrom.Value);
        }

        if (searchDto.DateTo.HasValue)
        {
            query = query.Where(s => s.StartUtc <= searchDto.DateTo.Value);
        }

        if (!string.IsNullOrEmpty(searchDto.Language))
        {
            query = query.Where(s => s.Language == searchDto.Language);
        }

        if (!string.IsNullOrEmpty(searchDto.Format))
        {
            query = query.Where(s => s.Format.ToString() == searchDto.Format);
        }

        // Get total count
        var total = await query.CountAsync(ct);

        // Apply sorting
        query = searchDto.SortBy.ToLower() switch
        {
            "startutc" => searchDto.SortDirection.ToLower() == "desc" 
                ? query.OrderByDescending(s => s.StartUtc)
                : query.OrderBy(s => s.StartUtc),
            "movie" => searchDto.SortDirection.ToLower() == "desc"
                ? query.OrderByDescending(s => s.Movie.Title)
                : query.OrderBy(s => s.Movie.Title),
            "cinema" => searchDto.SortDirection.ToLower() == "desc"
                ? query.OrderByDescending(s => s.Room.Cinema.Name)
                : query.OrderBy(s => s.Room.Cinema.Name),
            "room" => searchDto.SortDirection.ToLower() == "desc"
                ? query.OrderByDescending(s => s.Room.Name)
                : query.OrderBy(s => s.Room.Name),
            _ => query.OrderBy(s => s.StartUtc)
        };

        // Apply pagination
        var showtimes = await query
            .Skip((searchDto.Page - 1) * searchDto.PageSize)
            .Take(searchDto.PageSize)
            .ToListAsync(ct);

        return (showtimes, total);
    }

    public async Task<List<Showtime>> ListByMovieIdAsync(Guid movieId, CancellationToken ct = default)
    {
        return await _db.Showtimes
            .AsNoTracking()
            .Include(s => s.Movie)
            .Include(s => s.Room)
            .ThenInclude(r => r.Cinema)
            .Where(s => s.MovieId == movieId && s.StartUtc >= DateTime.UtcNow)
            .OrderBy(s => s.StartUtc)
            .ToListAsync(ct);
    }


    public async Task<Showtime?> AddAsync(Showtime showtime, CancellationToken ct = default)
    {
        _db.Showtimes.Add(showtime);
        await _db.SaveChangesAsync(ct);
        return showtime;
    }

    public async Task<Showtime?> UpdateAsync(Showtime showtime, CancellationToken ct = default)
    {
        _db.Showtimes.Update(showtime);
        await _db.SaveChangesAsync(ct);
        return showtime;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var showtime = await _db.Showtimes.FindAsync(new object[] { id }, ct);
        if (showtime == null) return false;

        _db.Showtimes.Remove(showtime);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Showtimes.AnyAsync(s => s.Id == id, ct);
    }

    public async Task<bool> HasTimeConflictAsync(Guid roomId, DateTime startUtc, DateTime endUtc, Guid? excludeId = null, CancellationToken ct = default)
    {
        var query = _db.Showtimes.Where(s => s.RoomId == roomId);

        if (excludeId.HasValue)
        {
            query = query.Where(s => s.Id != excludeId.Value);
        }

        // Check for time conflicts
        return await query.AnyAsync(s => 
            (startUtc >= s.StartUtc && startUtc < s.EndUtc) ||
            (endUtc > s.StartUtc && endUtc <= s.EndUtc) ||
            (startUtc <= s.StartUtc && endUtc >= s.EndUtc), ct);
    }


    public async Task<int> GetAvailableSeatsCountAsync(Guid showtimeId, CancellationToken ct = default)
    {
        var showtime = await _db.Showtimes
            .Include(s => s.Room)
            .ThenInclude(r => r.Seats)
            .FirstOrDefaultAsync(s => s.Id == showtimeId, ct);

        if (showtime == null) return 0;

        // Get booked seats count
        var bookedSeatsCount = await _db.BookingItems
            .Where(bi => bi.ShowtimeId == showtimeId && 
                        (bi.Status == BookingItemStatus.Held || bi.Status == BookingItemStatus.Confirmed))
            .CountAsync(ct);

        // Get total active seats
        var totalActiveSeats = showtime.Room.Seats.Count(s => s.IsActive);

        return totalActiveSeats - bookedSeatsCount;
    }

}

using be_movie_booking.Data;
using be_movie_booking.DTOs;
using be_movie_booking.Models;
using Microsoft.EntityFrameworkCore;

namespace be_movie_booking.Repositories;

/// <summary>
/// Interface cho Room Repository
/// </summary>
public interface IRoomRepository
{
    Task<Room?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Room?> GetByIdWithCinemaAsync(Guid id, CancellationToken ct = default);
    Task<(List<Room> rooms, int total)> ListByCinemaAsync(Guid cinemaId, RoomSearchDto searchDto, CancellationToken ct = default);
    Task<Room?> AddAsync(Room room, CancellationToken ct = default);
    Task<Room?> UpdateAsync(Room room, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByCodeAsync(string code, Guid cinemaId, CancellationToken ct = default);
    Task<bool> ExistsByCodeAsync(string code, Guid cinemaId, Guid excludeId, CancellationToken ct = default);
    Task<bool> HasActiveShowtimesAsync(Guid id, CancellationToken ct = default);
    Task<RoomStatsDto> GetStatsAsync(Guid id, CancellationToken ct = default);
}

/// <summary>
/// Repository để xử lý data access cho Room
/// </summary>
public class RoomRepository : IRoomRepository
{
    private readonly MovieBookingDbContext _db;

    public RoomRepository(MovieBookingDbContext db)
    {
        _db = db;
    }

    public Task<Room?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return _db.Rooms.FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public Task<Room?> GetByIdWithCinemaAsync(Guid id, CancellationToken ct = default)
    {
        return _db.Rooms
            .Include(r => r.Cinema)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<(List<Room> rooms, int total)> ListByCinemaAsync(Guid cinemaId, RoomSearchDto searchDto, CancellationToken ct = default)
    {
        var query = _db.Rooms
            .Include(r => r.Cinema)
            .Where(r => r.CinemaId == cinemaId);

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(searchDto.Search))
        {
            var searchTerm = searchDto.Search.ToLower();
            query = query.Where(r => 
                r.Name.ToLower().Contains(searchTerm) ||
                r.Code.ToLower().Contains(searchTerm));
        }

        // Apply status filter
        if (!string.IsNullOrWhiteSpace(searchDto.Status))
        {
            if (Enum.TryParse<EntityStatus>(searchDto.Status, true, out var status))
            {
                query = query.Where(r => r.Status == status);
            }
        }

        // Get total count before pagination
        var total = await query.CountAsync(ct);

        // Apply sorting
        query = searchDto.SortBy.ToLower() switch
        {
            "name" => searchDto.SortDirection.ToLower() == "asc" 
                ? query.OrderBy(r => r.Name)
                : query.OrderByDescending(r => r.Name),
            "code" => searchDto.SortDirection.ToLower() == "asc"
                ? query.OrderBy(r => r.Code)
                : query.OrderByDescending(r => r.Code),
            "createdat" => searchDto.SortDirection.ToLower() == "asc"
                ? query.OrderBy(r => r.CreatedAt)
                : query.OrderByDescending(r => r.CreatedAt),
            _ => query.OrderByDescending(r => r.CreatedAt)
        };

        // Apply pagination
        var rooms = await query
            .Skip((searchDto.Page - 1) * searchDto.PageSize)
            .Take(searchDto.PageSize)
            .ToListAsync(ct);

        return (rooms, total);
    }

    public async Task<Room?> AddAsync(Room room, CancellationToken ct = default)
    {
        await _db.Rooms.AddAsync(room, ct);
        await _db.SaveChangesAsync(ct);
        return room;
    }

    public async Task<Room?> UpdateAsync(Room room, CancellationToken ct = default)
    {
        _db.Rooms.Update(room);
        await _db.SaveChangesAsync(ct);
        return room;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (room == null) return false;

        // Soft delete: change status to Inactive
        room.Status = EntityStatus.Inactive;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        return _db.Rooms.AnyAsync(r => r.Id == id, ct);
    }

    public Task<bool> ExistsByCodeAsync(string code, Guid cinemaId, CancellationToken ct = default)
    {
        return _db.Rooms.AnyAsync(r => r.Code.ToLower() == code.ToLower() && r.CinemaId == cinemaId, ct);
    }

    public Task<bool> ExistsByCodeAsync(string code, Guid cinemaId, Guid excludeId, CancellationToken ct = default)
    {
        return _db.Rooms.AnyAsync(r => r.Code.ToLower() == code.ToLower() && r.CinemaId == cinemaId && r.Id != excludeId, ct);
    }

    public Task<bool> HasActiveShowtimesAsync(Guid id, CancellationToken ct = default)
    {
        return _db.Showtimes
            .AnyAsync(s => s.RoomId == id && s.StartUtc > DateTime.UtcNow, ct);
    }

    public async Task<RoomStatsDto> GetStatsAsync(Guid id, CancellationToken ct = default)
    {
        var room = await _db.Rooms
            .Include(r => r.Seats)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (room == null) return new RoomStatsDto();

        var showtimeStats = await _db.Showtimes
            .Where(s => s.RoomId == id)
            .GroupBy(s => s.StartUtc > DateTime.UtcNow)
            .Select(g => new { IsActive = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var activeShowtimes = showtimeStats.FirstOrDefault(s => s.IsActive)?.Count ?? 0;
        var totalShowtimes = showtimeStats.Sum(s => s.Count);

        return new RoomStatsDto
        {
            TotalSeats = room.TotalSeats,
            ActiveShowtimes = activeShowtimes,
            TotalShowtimes = totalShowtimes
        };
    }
}

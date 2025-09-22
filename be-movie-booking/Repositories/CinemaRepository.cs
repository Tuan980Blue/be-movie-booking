using be_movie_booking.Data;
using be_movie_booking.DTOs;
using be_movie_booking.Models;
using Microsoft.EntityFrameworkCore;

namespace be_movie_booking.Repositories;

/// <summary>
/// Interface cho Cinema Repository
/// </summary>
public interface ICinemaRepository
{
    Task<Cinema?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Cinema?> GetByIdWithRoomsAsync(Guid id, CancellationToken ct = default);
    Task<(List<Cinema> cinemas, int total)> ListAsync(CinemaSearchDto searchDto, CancellationToken ct = default);
    Task<Cinema?> AddAsync(Cinema cinema, CancellationToken ct = default);
    Task<Cinema?> UpdateAsync(Cinema cinema, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, Guid excludeId, CancellationToken ct = default);
    Task<bool> HasActiveRoomsAsync(Guid id, CancellationToken ct = default);
    Task<CinemaStatsDto> GetStatsAsync(Guid id, CancellationToken ct = default);
}

/// <summary>
/// Repository để xử lý data access cho Cinema
/// </summary>
public class CinemaRepository : ICinemaRepository
{
    private readonly MovieBookingDbContext _db;

    public CinemaRepository(MovieBookingDbContext db)
    {
        _db = db;
    }

    public Task<Cinema?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return _db.Cinemas.FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public Task<Cinema?> GetByIdWithRoomsAsync(Guid id, CancellationToken ct = default)
    {
        return _db.Cinemas
            .Include(c => c.Rooms)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<(List<Cinema> cinemas, int total)> ListAsync(CinemaSearchDto searchDto, CancellationToken ct = default)
    {
        var query = _db.Cinemas.AsQueryable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(searchDto.Search))
        {
            var searchTerm = searchDto.Search.ToLower();
            query = query.Where(c => 
                c.Name.ToLower().Contains(searchTerm) ||
                c.Address.ToLower().Contains(searchTerm) ||
                c.City.ToLower().Contains(searchTerm));
        }

        // Apply city filter
        if (!string.IsNullOrWhiteSpace(searchDto.City))
        {
            query = query.Where(c => c.City.ToLower().Contains(searchDto.City.ToLower()));
        }

        // Apply status filter
        if (!string.IsNullOrWhiteSpace(searchDto.Status))
        {
            if (Enum.TryParse<EntityStatus>(searchDto.Status, true, out var status))
            {
                query = query.Where(c => c.Status == status);
            }
        }

        // Get total count before pagination
        var total = await query.CountAsync(ct);

        // Apply sorting
        query = searchDto.SortBy.ToLower() switch
        {
            "name" => searchDto.SortDirection.ToLower() == "asc" 
                ? query.OrderBy(c => c.Name)
                : query.OrderByDescending(c => c.Name),
            "city" => searchDto.SortDirection.ToLower() == "asc"
                ? query.OrderBy(c => c.City)
                : query.OrderByDescending(c => c.City),
            "createdat" => searchDto.SortDirection.ToLower() == "asc"
                ? query.OrderBy(c => c.CreatedAt)
                : query.OrderByDescending(c => c.CreatedAt),
            _ => query.OrderByDescending(c => c.CreatedAt)
        };

        // Apply pagination
        var cinemas = await query
            .Skip((searchDto.Page - 1) * searchDto.PageSize)
            .Take(searchDto.PageSize)
            .ToListAsync(ct);

        return (cinemas, total);
    }

    public async Task<Cinema?> AddAsync(Cinema cinema, CancellationToken ct = default)
    {
        await _db.Cinemas.AddAsync(cinema, ct);
        await _db.SaveChangesAsync(ct);
        return cinema;
    }

    public async Task<Cinema?> UpdateAsync(Cinema cinema, CancellationToken ct = default)
    {
        _db.Cinemas.Update(cinema);
        await _db.SaveChangesAsync(ct);
        return cinema;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var cinema = await _db.Cinemas.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (cinema == null) return false;

        // Soft delete: change status to Inactive
        cinema.Status = EntityStatus.Inactive;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        return _db.Cinemas.AnyAsync(c => c.Id == id, ct);
    }

    public Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default)
    {
        return _db.Cinemas.AnyAsync(c => c.Name.ToLower() == name.ToLower(), ct);
    }

    public Task<bool> ExistsByNameAsync(string name, Guid excludeId, CancellationToken ct = default)
    {
        return _db.Cinemas.AnyAsync(c => c.Name.ToLower() == name.ToLower() && c.Id != excludeId, ct);
    }

    public Task<bool> HasActiveRoomsAsync(Guid id, CancellationToken ct = default)
    {
        return _db.Cinemas
            .Where(c => c.Id == id)
            .SelectMany(c => c.Rooms)
            .AnyAsync(r => r.Status == EntityStatus.Active, ct);
    }

    public async Task<CinemaStatsDto> GetStatsAsync(Guid id, CancellationToken ct = default)
    {
        var stats = await _db.Cinemas
            .Where(c => c.Id == id)
            .Select(c => new CinemaStatsDto
            {
                TotalRooms = c.Rooms.Count,
                ActiveRooms = c.Rooms.Count(r => r.Status == EntityStatus.Active),
                InactiveRooms = c.Rooms.Count(r => r.Status == EntityStatus.Inactive),
                TotalSeats = c.Rooms.Sum(r => r.TotalSeats)
            })
            .FirstOrDefaultAsync(ct);

        return stats ?? new CinemaStatsDto();
    }
}

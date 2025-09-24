using be_movie_booking.DTOs;
using be_movie_booking.Models;
using be_movie_booking.Data;
using Microsoft.EntityFrameworkCore;

namespace be_movie_booking.Repositories;

/// <summary>
/// Interface cho Seat Repository
/// </summary>
public interface ISeatRepository
{
    Task<Seat?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Seat?> GetByIdWithRoomAsync(Guid id, CancellationToken ct = default);
    Task<List<Seat>> ListByRoomAsync(Guid roomId, SeatSearchDto searchDto, CancellationToken ct = default);
    Task<(List<Seat> items, int total)> ListByRoomWithPagingAsync(Guid roomId, SeatSearchDto searchDto, CancellationToken ct = default);
    Task<SeatLayoutDto> GetSeatLayoutAsync(Guid roomId, CancellationToken ct = default);
    Task<Seat?> CreateAsync(Seat seat, CancellationToken ct = default);
    Task<List<Seat>> CreateBulkAsync(List<Seat> seats, CancellationToken ct = default);
    Task<Seat?> UpdateAsync(Seat seat, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<bool> DeleteByRoomAsync(Guid roomId, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid roomId, string rowLabel, int seatNumber, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid roomId, string rowLabel, int seatNumber, Guid excludeId, CancellationToken ct = default);
    Task<SeatStatsDto> GetStatsAsync(Guid roomId, CancellationToken ct = default);
    Task<List<Seat>> GetAvailableSeatsAsync(Guid roomId, CancellationToken ct = default);
    Task<List<Seat>> GetSeatsByTypeAsync(Guid roomId, SeatType seatType, CancellationToken ct = default);
    Task<List<Seat>> GetByIdsAsync(List<Guid> ids, CancellationToken ct = default);
}

/// <summary>
/// Repository để xử lý data access cho Seat
/// </summary>
public class SeatRepository : ISeatRepository
{
    private readonly MovieBookingDbContext _context;

    public SeatRepository(MovieBookingDbContext context)
    {
        _context = context;
    }

    public async Task<Seat?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Seats
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<Seat?> GetByIdWithRoomAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Seats
            .Include(x => x.Room)
            .ThenInclude(r => r.Cinema)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<List<Seat>> ListByRoomAsync(Guid roomId, SeatSearchDto searchDto, CancellationToken ct = default)
    {
        var query = _context.Seats
            .Where(x => x.RoomId == roomId);

        // Apply filters
        if (!string.IsNullOrEmpty(searchDto.RowLabel))
        {
            query = query.Where(x => x.RowLabel.Contains(searchDto.RowLabel));
        }

        if (searchDto.SeatNumber.HasValue)
        {
            query = query.Where(x => x.SeatNumber == searchDto.SeatNumber.Value);
        }

        if (!string.IsNullOrEmpty(searchDto.SeatType))
        {
            if (Enum.TryParse<SeatType>(searchDto.SeatType, out var seatType))
            {
                query = query.Where(x => x.SeatType == seatType);
            }
        }

        if (searchDto.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == searchDto.IsActive.Value);
        }

        // Apply sorting
        query = searchDto.SortBy.ToLower() switch
        {
            "rowlabel" => searchDto.SortDirection.ToLower() == "desc" 
                ? query.OrderByDescending(x => x.RowLabel)
                : query.OrderBy(x => x.RowLabel),
            "seatnumber" => searchDto.SortDirection.ToLower() == "desc"
                ? query.OrderByDescending(x => x.SeatNumber)
                : query.OrderBy(x => x.SeatNumber),
            "seattype" => searchDto.SortDirection.ToLower() == "desc"
                ? query.OrderByDescending(x => x.SeatType)
                : query.OrderBy(x => x.SeatType),
            "createdat" => searchDto.SortDirection.ToLower() == "desc"
                ? query.OrderByDescending(x => x.CreatedAt)
                : query.OrderBy(x => x.CreatedAt),
            _ => query.OrderBy(x => x.RowLabel).ThenBy(x => x.SeatNumber)
        };

        return await query.ToListAsync(ct);
    }

    public async Task<(List<Seat> items, int total)> ListByRoomWithPagingAsync(Guid roomId, SeatSearchDto searchDto, CancellationToken ct = default)
    {
        var query = _context.Seats
            .Where(x => x.RoomId == roomId);

        // Apply filters (same as ListByRoomAsync)
        if (!string.IsNullOrEmpty(searchDto.RowLabel))
        {
            query = query.Where(x => x.RowLabel.Contains(searchDto.RowLabel));
        }

        if (searchDto.SeatNumber.HasValue)
        {
            query = query.Where(x => x.SeatNumber == searchDto.SeatNumber.Value);
        }

        if (!string.IsNullOrEmpty(searchDto.SeatType))
        {
            if (Enum.TryParse<SeatType>(searchDto.SeatType, out var seatType))
            {
                query = query.Where(x => x.SeatType == seatType);
            }
        }

        if (searchDto.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == searchDto.IsActive.Value);
        }

        // Get total count
        var total = await query.CountAsync(ct);

        // Apply sorting
        query = searchDto.SortBy.ToLower() switch
        {
            "rowlabel" => searchDto.SortDirection.ToLower() == "desc" 
                ? query.OrderByDescending(x => x.RowLabel)
                : query.OrderBy(x => x.RowLabel),
            "seatnumber" => searchDto.SortDirection.ToLower() == "desc"
                ? query.OrderByDescending(x => x.SeatNumber)
                : query.OrderBy(x => x.SeatNumber),
            "seattype" => searchDto.SortDirection.ToLower() == "desc"
                ? query.OrderByDescending(x => x.SeatType)
                : query.OrderBy(x => x.SeatType),
            "createdat" => searchDto.SortDirection.ToLower() == "desc"
                ? query.OrderByDescending(x => x.CreatedAt)
                : query.OrderBy(x => x.CreatedAt),
            _ => query.OrderBy(x => x.RowLabel).ThenBy(x => x.SeatNumber)
        };

        // Apply paging
        var items = await query
            .Skip((searchDto.Page - 1) * searchDto.PageSize)
            .Take(searchDto.PageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<SeatLayoutDto> GetSeatLayoutAsync(Guid roomId, CancellationToken ct = default)
    {
        var room = await _context.Rooms
            .Include(r => r.Cinema)
            .FirstOrDefaultAsync(r => r.Id == roomId, ct);

        if (room == null)
        {
            throw new ArgumentException("Room not found", nameof(roomId));
        }

        var seats = await _context.Seats
            .Where(s => s.RoomId == roomId)
            .OrderBy(s => s.RowLabel)
            .ThenBy(s => s.SeatNumber)
            .ToListAsync(ct);

        var rows = seats
            .GroupBy(s => s.RowLabel)
            .Select(g => new SeatRowDto
            {
                RowLabel = g.Key,
                Seats = g.Select(s => new SeatDto
                {
                    Id = s.Id,
                    RowLabel = s.RowLabel,
                    SeatNumber = s.SeatNumber,
                    SeatType = s.SeatType.ToString(),
                    IsActive = s.IsActive,
                    PositionX = s.PositionX,
                    PositionY = s.PositionY,
                    SpecialNotes = s.SpecialNotes
                }).ToList()
            })
            .ToList();

        var seatTypes = new List<SeatTypeInfoDto>
        {
            new() { Type = "Standard", Name = "Ghế thường", Color = "#6B7280", Description = "Ghế ngồi tiêu chuẩn" },
            new() { Type = "Vip", Name = "Ghế VIP", Color = "#F59E0B", Description = "Ghế VIP với không gian rộng rãi" },
            new() { Type = "Couple", Name = "Ghế đôi", Color = "#EC4899", Description = "Ghế đôi cho cặp đôi" },
            new() { Type = "Accessible", Name = "Ghế khuyết tật", Color = "#10B981", Description = "Ghế dành cho người khuyết tật" }
        };

        return new SeatLayoutDto
        {
            RoomId = roomId,
            RoomName = room.Name,
            Rows = rows,
            SeatTypes = seatTypes,
            ScreenPosition = "front"
        };
    }

    public async Task<Seat?> CreateAsync(Seat seat, CancellationToken ct = default)
    {
        _context.Seats.Add(seat);
        await _context.SaveChangesAsync(ct);
        return seat;
    }

    public async Task<List<Seat>> CreateBulkAsync(List<Seat> seats, CancellationToken ct = default)
    {
        _context.Seats.AddRange(seats);
        await _context.SaveChangesAsync(ct);
        return seats;
    }

    public async Task<Seat?> UpdateAsync(Seat seat, CancellationToken ct = default)
    {
        _context.Seats.Update(seat);
        await _context.SaveChangesAsync(ct);
        return seat;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var seat = await _context.Seats.FindAsync(id);
        if (seat == null) return false;

        _context.Seats.Remove(seat);
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteByRoomAsync(Guid roomId, CancellationToken ct = default)
    {
        var seats = await _context.Seats.Where(s => s.RoomId == roomId).ToListAsync(ct);
        if (!seats.Any()) return false;

        _context.Seats.RemoveRange(seats);
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ExistsAsync(Guid roomId, string rowLabel, int seatNumber, CancellationToken ct = default)
    {
        return await _context.Seats
            .AnyAsync(x => x.RoomId == roomId && x.RowLabel == rowLabel && x.SeatNumber == seatNumber, ct);
    }

    public async Task<bool> ExistsAsync(Guid roomId, string rowLabel, int seatNumber, Guid excludeId, CancellationToken ct = default)
    {
        return await _context.Seats
            .AnyAsync(x => x.RoomId == roomId && x.RowLabel == rowLabel && x.SeatNumber == seatNumber && x.Id != excludeId, ct);
    }

    public async Task<SeatStatsDto> GetStatsAsync(Guid roomId, CancellationToken ct = default)
    {
        var seats = await _context.Seats
            .Where(s => s.RoomId == roomId)
            .ToListAsync(ct);

        var totalSeats = seats.Count;
        var activeSeats = seats.Count(s => s.IsActive);

        var seatsByType = seats
            .GroupBy(s => s.SeatType.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        return new SeatStatsDto
        {
            TotalSeats = totalSeats,
            ActiveSeats = activeSeats,
            SeatsByType = seatsByType
        };
    }

    public async Task<List<Seat>> GetAvailableSeatsAsync(Guid roomId, CancellationToken ct = default)
    {
        return await _context.Seats
            .Where(s => s.RoomId == roomId && s.IsActive)
            .OrderBy(s => s.RowLabel)
            .ThenBy(s => s.SeatNumber)
            .ToListAsync(ct);
    }

    public async Task<List<Seat>> GetSeatsByTypeAsync(Guid roomId, SeatType seatType, CancellationToken ct = default)
    {
        return await _context.Seats
            .Where(s => s.RoomId == roomId && s.SeatType == seatType && s.IsActive)
            .OrderBy(s => s.RowLabel)
            .ThenBy(s => s.SeatNumber)
            .ToListAsync(ct);
    }

    public async Task<List<Seat>> GetByIdsAsync(List<Guid> ids, CancellationToken ct = default)
    {
        return await _context.Seats
            .Where(s => ids.Contains(s.Id))
            .ToListAsync(ct);
    }
}

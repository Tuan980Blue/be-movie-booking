using be_movie_booking.Data;
using be_movie_booking.DTOs;
using be_movie_booking.Models;
using Microsoft.EntityFrameworkCore;

namespace be_movie_booking.Repositories;

/// <summary>
/// Interface cho Booking Repository
/// </summary>
public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Booking?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<Booking?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<(List<be_movie_booking.DTOs.BookingListItemDto> items, int total)> ListAsync(BookingSearchDto searchDto, CancellationToken ct = default);
    Task<Booking?> AddAsync(Booking booking, CancellationToken ct = default);
    Task<Booking?> UpdateAsync(Booking booking, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
    Task<bool> AreSeatsBookedAsync(Guid showtimeId, List<Guid> seatIds, CancellationToken ct = default);
    Task<string> GenerateUniqueBookingCodeAsync(CancellationToken ct = default);
    Task<string> GenerateUniqueTicketCodeAsync(CancellationToken ct = default);
}

/// <summary>
/// Repository để xử lý data access cho Booking
/// </summary>
public class BookingRepository : IBookingRepository
{
    private readonly MovieBookingDbContext _db;

    public BookingRepository(MovieBookingDbContext db)
    {
        _db = db;
    }

    public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Bookings
            .Include(b => b.User)
            .Include(b => b.Items)
                .ThenInclude(i => i.Showtime)
                    .ThenInclude(s => s.Movie)
            .Include(b => b.Items)
                .ThenInclude(i => i.Showtime)
                    .ThenInclude(s => s.Room)
                        .ThenInclude(r => r.Cinema)
            .Include(b => b.Items)
                .ThenInclude(i => i.Seat)
            .Include(b => b.Tickets)
            .FirstOrDefaultAsync(b => b.Id == id, ct);
    }

    public async Task<Booking?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Bookings
            .Include(b => b.User)
            .Include(b => b.Items)
                .ThenInclude(i => i.Showtime)
                    .ThenInclude(s => s.Movie)
            .Include(b => b.Items)
                .ThenInclude(i => i.Showtime)
                    .ThenInclude(s => s.Room)
                        .ThenInclude(r => r.Cinema)
            .Include(b => b.Items)
                .ThenInclude(i => i.Seat)
            .Include(b => b.Tickets)
            .FirstOrDefaultAsync(b => b.Id == id, ct);
    }

    public async Task<Booking?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        return await _db.Bookings
            .Include(b => b.User)
            .Include(b => b.Items)
                .ThenInclude(i => i.Showtime)
                    .ThenInclude(s => s.Movie)
            .Include(b => b.Items)
                .ThenInclude(i => i.Showtime)
                    .ThenInclude(s => s.Room)
                        .ThenInclude(r => r.Cinema)
            .Include(b => b.Items)
                .ThenInclude(i => i.Seat)
            .Include(b => b.Tickets)
            .FirstOrDefaultAsync(b => b.Code == code, ct);
    }

    public async Task<(List<be_movie_booking.DTOs.BookingListItemDto> items, int total)> ListAsync(BookingSearchDto searchDto, CancellationToken ct = default)
    {
        var q = _db.Bookings.AsQueryable();

        if (searchDto.UserId.HasValue)
        {
            q = q.Where(b => b.UserId == searchDto.UserId.Value);
        }
        if (searchDto.Status.HasValue)
        {
            q = q.Where(b => b.Status == searchDto.Status.Value);
        }
        if (searchDto.DateFrom.HasValue)
        {
            q = q.Where(b => b.CreatedAt >= searchDto.DateFrom.Value);
        }
        if (searchDto.DateTo.HasValue)
        {
            q = q.Where(b => b.CreatedAt <= searchDto.DateTo.Value);
        }

        var total = await q.CountAsync(ct);

        q = searchDto.SortBy.ToLower() switch
        {
            "code" => searchDto.SortOrder.ToLower() == "asc" ? q.OrderBy(b => b.Code) : q.OrderByDescending(b => b.Code),
            "createdat" => searchDto.SortOrder.ToLower() == "asc" ? q.OrderBy(b => b.CreatedAt) : q.OrderByDescending(b => b.CreatedAt),
            "status" => searchDto.SortOrder.ToLower() == "asc" ? q.OrderBy(b => b.Status) : q.OrderByDescending(b => b.Status),
            _ => q.OrderByDescending(b => b.CreatedAt)
        };

        var items = await q
            .Skip((searchDto.Page - 1) * searchDto.PageSize)
            .Take(searchDto.PageSize)
            .Select(b => new be_movie_booking.DTOs.BookingListItemDto
            {
                Id = b.Id,
                Code = b.Code,
                Currency = b.Currency,
                TotalAmountMinor = b.TotalAmountMinor,
                Status = b.Status,
                CreatedAt = b.CreatedAt,
                SeatsCount = b.Items.Count,
                MovieTitle = b.Items
                    .OrderBy(i => i.CreatedAt)
                    .Select(i => i.Showtime.Movie.Title)
                    .FirstOrDefault(),
                StartUtc = b.Items
                    .OrderBy(i => i.CreatedAt)
                    .Select(i => i.Showtime.StartUtc)
                    .FirstOrDefault(),
                CinemaName = b.Items
                    .OrderBy(i => i.CreatedAt)
                    .Select(i => i.Showtime.Room.Cinema.Name)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<Booking?> AddAsync(Booking booking, CancellationToken ct = default)
    {
        await _db.Bookings.AddAsync(booking, ct);
        await _db.SaveChangesAsync(ct);
        return booking;
    }

    public async Task<Booking?> UpdateAsync(Booking booking, CancellationToken ct = default)
    {
        booking.UpdatedAt = DateTime.UtcNow;
        _db.Bookings.Update(booking);
        await _db.SaveChangesAsync(ct);
        return booking;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Bookings.AnyAsync(b => b.Id == id, ct);
    }

    public async Task<bool> AreSeatsBookedAsync(Guid showtimeId, List<Guid> seatIds, CancellationToken ct = default)
    {
        // Mục đích: Kiểm tra xem có bất kỳ ghế nào trong danh sách đã được đặt cho suất chiếu cụ thể không
        var blockingStatuses = new[] { BookingItemStatus.Pending, BookingItemStatus.Confirmed };

        var bookedSeats = await _db.BookingItems
            .Where(item => item.ShowtimeId == showtimeId
                && seatIds.Contains(item.SeatId)
                && blockingStatuses.Contains(item.Status))
            .Select(item => item.SeatId)
            .ToListAsync(ct);

        return bookedSeats.Any();
    }

    public async Task<string> GenerateUniqueBookingCodeAsync(CancellationToken ct = default)
    {
        string code;
        bool exists;
        do
        {
            // Generate 8-character alphanumeric code
            code = GenerateRandomCode(8);
            exists = await _db.Bookings.AnyAsync(b => b.Code == code, ct);
        } while (exists);

        return code;
    }

    public async Task<string> GenerateUniqueTicketCodeAsync(CancellationToken ct = default)
    {
        string code;
        bool exists;
        do
        {
            // Generate 10-character alphanumeric code
            code = GenerateRandomCode(10);
            exists = await _db.Tickets.AnyAsync(t => t.TicketCode == code, ct);
        } while (exists);

        return code;
    }

    private static string GenerateRandomCode(int length)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Exclude similar-looking characters
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}

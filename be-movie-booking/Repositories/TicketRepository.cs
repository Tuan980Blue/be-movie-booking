using be_movie_booking.Data;
using be_movie_booking.Models;
using Microsoft.EntityFrameworkCore;

namespace be_movie_booking.Repositories;

/// <summary>
/// Interface cho Ticket Repository
/// </summary>
public interface ITicketRepository
{
    Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Ticket?> GetByTicketCodeAsync(string ticketCode, CancellationToken ct = default);
    Task<Ticket?> GetByTicketCodeWithDetailsAsync(string ticketCode, CancellationToken ct = default);
    Task<Ticket?> UpdateAsync(Ticket ticket, CancellationToken ct = default);
}

/// <summary>
/// Repository để xử lý data access cho Ticket
/// </summary>
public class TicketRepository : ITicketRepository
{
    private readonly MovieBookingDbContext _db;

    public TicketRepository(MovieBookingDbContext db)
    {
        _db = db;
    }

    public async Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Tickets
            .Include(t => t.Booking)
            .Include(t => t.Showtime)
                .ThenInclude(s => s.Movie)
            .Include(t => t.Showtime)
                .ThenInclude(s => s.Room)
                    .ThenInclude(r => r.Cinema)
            .Include(t => t.Seat)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<Ticket?> GetByTicketCodeAsync(string ticketCode, CancellationToken ct = default)
    {
        return await _db.Tickets
            .Include(t => t.Booking)
            .Include(t => t.Showtime)
                .ThenInclude(s => s.Movie)
            .Include(t => t.Showtime)
                .ThenInclude(s => s.Room)
                    .ThenInclude(r => r.Cinema)
            .Include(t => t.Seat)
            .FirstOrDefaultAsync(t => t.TicketCode == ticketCode, ct);
    }

    public async Task<Ticket?> GetByTicketCodeWithDetailsAsync(string ticketCode, CancellationToken ct = default)
    {
        return await _db.Tickets
            .Include(t => t.Booking)
                .ThenInclude(b => b.User)
            .Include(t => t.Showtime)
                .ThenInclude(s => s.Movie)
            .Include(t => t.Showtime)
                .ThenInclude(s => s.Room)
                    .ThenInclude(r => r.Cinema)
            .Include(t => t.Seat)
            .FirstOrDefaultAsync(t => t.TicketCode == ticketCode, ct);
    }

    public async Task<Ticket?> UpdateAsync(Ticket ticket, CancellationToken ct = default)
    {
        _db.Tickets.Update(ticket);
        await _db.SaveChangesAsync(ct);
        return ticket;
    }
}


using be_movie_booking.Data;
using be_movie_booking.Models;
using Microsoft.EntityFrameworkCore;

namespace be_movie_booking.Repositories;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Payment?> GetByIdWithBookingAsync(Guid id, CancellationToken ct = default);
    Task<Payment?> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default);
    Task<Payment> CreateAsync(Payment payment, CancellationToken ct = default);
    Task<Payment> UpdateAsync(Payment payment, CancellationToken ct = default);
    Task<PaymentEvent> AddEventAsync(PaymentEvent paymentEvent, CancellationToken ct = default);
    Task<(List<Payment> Items, int Total)> SearchAsync(
        Guid? bookingId,
        PaymentProvider? provider,
        PaymentStatus? status,
        int page,
        int pageSize,
        string sortBy,
        string sortOrder,
        CancellationToken ct = default
    );
}

public class PaymentRepository : IPaymentRepository
{
    private readonly MovieBookingDbContext _context;

    public PaymentRepository(MovieBookingDbContext context)
    {
        _context = context;
    }

    public async Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Payments
            .Include(p => p.Events)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<Payment?> GetByIdWithBookingAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Payments
            .Include(p => p.Booking)
            .Include(p => p.Events)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<Payment?> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default)
    {
        return await _context.Payments
            .Include(p => p.Events)
            .FirstOrDefaultAsync(p => p.BookingId == bookingId, ct);
    }

    public async Task<Payment> CreateAsync(Payment payment, CancellationToken ct = default)
    {
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(ct);
        return payment;
    }

    public async Task<Payment> UpdateAsync(Payment payment, CancellationToken ct = default)
    {
        payment.UpdatedAt = DateTime.UtcNow;
        _context.Payments.Update(payment);
        await _context.SaveChangesAsync(ct);
        return payment;
    }

    public async Task<PaymentEvent> AddEventAsync(PaymentEvent paymentEvent, CancellationToken ct = default)
    {
        _context.PaymentEvents.Add(paymentEvent);
        await _context.SaveChangesAsync(ct);
        return paymentEvent;
    }

    public async Task<(List<Payment> Items, int Total)> SearchAsync(
        Guid? bookingId,
        PaymentProvider? provider,
        PaymentStatus? status,
        int page,
        int pageSize,
        string sortBy,
        string sortOrder,
        CancellationToken ct = default)
    {
        var query = _context.Payments.AsQueryable();

        if (bookingId.HasValue)
        {
            query = query.Where(p => p.BookingId == bookingId.Value);
        }

        if (provider.HasValue)
        {
            query = query.Where(p => p.Provider == provider.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        var total = await query.CountAsync(ct);

        // Apply sorting
        query = sortBy.ToLower() switch
        {
            "createdat" => sortOrder.ToLower() == "asc"
                ? query.OrderBy(p => p.CreatedAt)
                : query.OrderByDescending(p => p.CreatedAt),
            "amount" => sortOrder.ToLower() == "asc"
                ? query.OrderBy(p => p.AmountMinor)
                : query.OrderByDescending(p => p.AmountMinor),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(p => p.Events)
            .ToListAsync(ct);

        return (items, total);
    }
}

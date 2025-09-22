using be_movie_booking.Data;
using be_movie_booking.DTOs;
using be_movie_booking.Models;
using Microsoft.EntityFrameworkCore;

namespace be_movie_booking.Repositories;

public interface IPriceRuleRepository
{
    Task<PriceRule?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(List<PriceRule> items, int total)> ListAsync(PriceRuleSearchDto search, CancellationToken ct = default);
    Task<PriceRule?> AddAsync(PriceRule rule, CancellationToken ct = default);
    Task<PriceRule?> UpdateAsync(PriceRule rule, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<PriceRule?> FindByKeyAsync(Guid? cinemaId, DayType dayType, SeatType seatType, CancellationToken ct = default);
}

public class PriceRuleRepository : IPriceRuleRepository
{
    private readonly MovieBookingDbContext _db;

    public PriceRuleRepository(MovieBookingDbContext db)
    {
        _db = db;
    }

    public Task<PriceRule?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return _db.PriceRules.FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<(List<PriceRule> items, int total)> ListAsync(PriceRuleSearchDto search, CancellationToken ct = default)
    {
        var q = _db.PriceRules.AsQueryable();

        if (search.CinemaId.HasValue)
        {
            q = q.Where(x => x.CinemaId == search.CinemaId);
        }
        else
        {
            // keep both global and all cinemas when null
        }

        if (search.DayType.HasValue)
        {
            q = q.Where(x => x.DayType == search.DayType.Value);
        }
        if (search.SeatType.HasValue)
        {
            q = q.Where(x => x.SeatType == search.SeatType.Value);
        }
        if (search.IsActive.HasValue)
        {
            q = q.Where(x => x.IsActive == search.IsActive.Value);
        }

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderBy(x => x.CinemaId)
            .ThenBy(x => x.DayType)
            .ThenBy(x => x.SeatType)
            .Skip((search.Page - 1) * search.PageSize)
            .Take(search.PageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<PriceRule?> AddAsync(PriceRule rule, CancellationToken ct = default)
    {
        await _db.PriceRules.AddAsync(rule, ct);
        await _db.SaveChangesAsync(ct);
        return rule;
    }

    public async Task<PriceRule?> UpdateAsync(PriceRule rule, CancellationToken ct = default)
    {
        _db.PriceRules.Update(rule);
        await _db.SaveChangesAsync(ct);
        return rule;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.PriceRules.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity == null) return false;
        _db.PriceRules.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public Task<PriceRule?> FindByKeyAsync(Guid? cinemaId, DayType dayType, SeatType seatType, CancellationToken ct = default)
    {
        return _db.PriceRules.FirstOrDefaultAsync(x => x.CinemaId == cinemaId && x.DayType == dayType && x.SeatType == seatType && x.IsActive, ct);
    }
}

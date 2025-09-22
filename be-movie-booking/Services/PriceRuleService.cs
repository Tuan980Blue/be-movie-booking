using be_movie_booking.Data;
using be_movie_booking.DTOs;
using be_movie_booking.Models;
using be_movie_booking.Repositories;
using Microsoft.EntityFrameworkCore;

namespace be_movie_booking.Services;

public interface IPriceRuleService
{
    Task<(List<PriceRuleResponseDto> items, int total)> ListAsync(PriceRuleSearchDto search, CancellationToken ct = default);
    Task<PriceRuleResponseDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<PriceRuleResponseDto> CreateAsync(PriceRuleCreateDto dto, CancellationToken ct = default);
    Task<PriceRuleResponseDto?> UpdateAsync(Guid id, PriceRuleUpdateDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface IPricingService
{
    Task<PricingQuoteResponseDto> QuoteAsync(PricingQuoteRequestDto request, CancellationToken ct = default);
}

public class PriceRuleService : IPriceRuleService
{
    private readonly IPriceRuleRepository _repo;

    public PriceRuleService(IPriceRuleRepository repo)
    {
        _repo = repo;
    }

    public async Task<(List<PriceRuleResponseDto> items, int total)> ListAsync(PriceRuleSearchDto search, CancellationToken ct = default)
    {
        var (items, total) = await _repo.ListAsync(search, ct);
        return (items.Select(ToDto).ToList(), total);
    }

    public async Task<PriceRuleResponseDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        return entity == null ? null : ToDto(entity);
    }

    public async Task<PriceRuleResponseDto> CreateAsync(PriceRuleCreateDto dto, CancellationToken ct = default)
    {
        // Basic validation
        if (!Enum.IsDefined(typeof(DayType), dto.DayType))
        {
            throw new ArgumentException("Invalid DayType");
        }
        if (!Enum.IsDefined(typeof(SeatType), dto.SeatType))
        {
            throw new ArgumentException("Invalid SeatType");
        }
        if (dto.PriceMinor <= 0)
        {
            throw new ArgumentException("PriceMinor must be greater than 0");
        }

        // Validate uniqueness: (CinemaId, DayType, SeatType) unique; and global unique when CinemaId is null
        var existed = await _repo.FindByKeyAsync(dto.CinemaId, dto.DayType, dto.SeatType, ct)
                     ?? (dto.CinemaId == null ? await _repo.FindByKeyAsync(null, dto.DayType, dto.SeatType, ct) : null);
        if (existed != null)
        {
            throw new InvalidOperationException("Quy định về giá đã tồn tại cho phạm vi, loại ngày và loại ghế nhất định");
        }
        var entity = new PriceRule
        {
            Id = Guid.NewGuid(),
            CinemaId = dto.CinemaId,
            DayType = dto.DayType,
            SeatType = dto.SeatType,
            PriceMinor = dto.PriceMinor,
            IsActive = dto.IsActive
        };
        await _repo.AddAsync(entity, ct);
        return ToDto(entity);
    }

    public async Task<PriceRuleResponseDto?> UpdateAsync(Guid id, PriceRuleUpdateDto dto, CancellationToken ct = default)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null) return null;

        // Basic validation
        if (!Enum.IsDefined(typeof(DayType), dto.DayType))
        {
            throw new ArgumentException("Invalid DayType");
        }
        if (!Enum.IsDefined(typeof(SeatType), dto.SeatType))
        {
            throw new ArgumentException("Invalid SeatType");
        }
        if (dto.PriceMinor <= 0)
        {
            throw new ArgumentException("PriceMinor must be greater than 0");
        }

        // Validate uniqueness for target
        if (entity.CinemaId != dto.CinemaId || entity.DayType != dto.DayType || entity.SeatType != dto.SeatType)
        {
            var existed = await _repo.FindByKeyAsync(dto.CinemaId, dto.DayType, dto.SeatType, ct)
                         ?? (dto.CinemaId == null ? await _repo.FindByKeyAsync(null, dto.DayType, dto.SeatType, ct) : null);
            if (existed != null && existed.Id != id)
            {
                throw new InvalidOperationException("Quy định về giá đã tồn tại cho phạm vi, loại ngày và loại ghế nhất định");
            }
        }

        entity.CinemaId = dto.CinemaId;
        entity.DayType = dto.DayType;
        entity.SeatType = dto.SeatType;
        entity.PriceMinor = dto.PriceMinor;
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(entity, ct);
        return ToDto(entity);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        return _repo.DeleteAsync(id, ct);
    }

    private static PriceRuleResponseDto ToDto(PriceRule e)
    {
        return new PriceRuleResponseDto
        {
            Id = e.Id,
            CinemaId = e.CinemaId,
            DayType = e.DayType,
            SeatType = e.SeatType,
            PriceMinor = e.PriceMinor,
            IsActive = e.IsActive,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };
    }
}

public class PricingService : IPricingService
{
    private readonly MovieBookingDbContext _db;
    private readonly IPriceRuleRepository _repo;

    public PricingService(MovieBookingDbContext db, IPriceRuleRepository repo)
    {
        _db = db;
        _repo = repo;
    }

    public async Task<PricingQuoteResponseDto> QuoteAsync(PricingQuoteRequestDto request, CancellationToken ct = default)
    {
        var st = await _db.Showtimes.Include(x => x.Room).ThenInclude(r => r.Cinema)
            .FirstOrDefaultAsync(x => x.Id == request.ShowtimeId, ct) ?? throw new KeyNotFoundException("Showtime not found");
        var seat = await _db.Seats.FirstOrDefaultAsync(x => x.Id == request.SeatId, ct) ?? throw new KeyNotFoundException("Seat not found");

        var dayType = IsWeekend(st.StartUtc) ? DayType.Weekend : DayType.Weekday;
        var cinemaId = st.Room.CinemaId;

        var rule = await _repo.FindByKeyAsync(cinemaId, dayType, seat.SeatType, ct)
                   ?? await _repo.FindByKeyAsync(null, dayType, seat.SeatType, ct);

        var price = rule?.PriceMinor ?? st.BasePriceMinor;

        return new PricingQuoteResponseDto
        {
            ShowtimeId = st.Id,
            SeatId = seat.Id,
            PriceMinor = price,
            Currency = "VND",
            DayType = dayType,
            SeatType = seat.SeatType,
            CinemaId = cinemaId,
            UsedGlobalRule = rule != null && rule.CinemaId == null
        };
    }

    private static bool IsWeekend(DateTime utc)
    {
        return utc.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    }
}

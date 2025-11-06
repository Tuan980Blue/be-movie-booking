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
    /// <summary>
    /// Tính giá cho nhiều ghế - chỉ cần SeatIds
    /// </summary>
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
        if (!Enum.IsDefined(typeof(SeatType), dto.SeatType))
        {
            throw new ArgumentException("Invalid SeatType");
        }
        if (dto.PriceMinor <= 0)
        {
            throw new ArgumentException("PriceMinor must be greater than 0");
        }

        // Validate uniqueness: mỗi SeatType chỉ có 1 PriceRule
        var existed = await _repo.FindBySeatTypeAsync(dto.SeatType, ct);
        if (existed != null)
        {
            throw new InvalidOperationException($"Quy định về giá cho loại ghế {dto.SeatType} đã tồn tại");
        }

        var entity = new PriceRule
        {
            Id = Guid.NewGuid(),
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
        if (!Enum.IsDefined(typeof(SeatType), dto.SeatType))
        {
            throw new ArgumentException("Invalid SeatType");
        }
        if (dto.PriceMinor <= 0)
        {
            throw new ArgumentException("PriceMinor must be greater than 0");
        }

        // Validate uniqueness for target SeatType
        if (entity.SeatType != dto.SeatType)
        {
            var existed = await _repo.FindBySeatTypeAsync(dto.SeatType, ct);
            if (existed != null && existed.Id != id)
            {
                throw new InvalidOperationException($"Quy định về giá cho loại ghế {dto.SeatType} đã tồn tại");
            }
        }

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
        if (request.SeatIds == null || !request.SeatIds.Any())
        {
            throw new ArgumentException("Danh sách ghế không được trống");
        }

        // Lấy tất cả ghế cùng lúc
        var seats = await _db.Seats
            .Where(s => request.SeatIds.Contains(s.Id))
            .ToListAsync(ct);

        if (seats.Count != request.SeatIds.Count)
        {
            throw new KeyNotFoundException("Một số ghế không tồn tại");
        }

        // Load tất cả PriceRule để cache
        var seatTypes = seats.Select(s => s.SeatType).Distinct().ToList();
        var priceRules = await _db.PriceRules
            .Where(pr => seatTypes.Contains(pr.SeatType) && pr.IsActive)
            .ToListAsync(ct);

        var quotes = new List<PricingQuoteItemDto>();
        var totalAmount = 0;

        foreach (var seat in seats)
        {
            var rule = priceRules.FirstOrDefault(r => r.SeatType == seat.SeatType)
                ?? throw new InvalidOperationException($"Không tìm thấy quy định giá cho loại ghế {seat.SeatType}");

            quotes.Add(new PricingQuoteItemDto
            {
                SeatId = seat.Id,
                SeatType = seat.SeatType,
                PriceMinor = rule.PriceMinor
            });

            totalAmount += rule.PriceMinor;
        }

        return new PricingQuoteResponseDto
        {
            Quotes = quotes,
            TotalAmountMinor = totalAmount,
            Currency = "VND"
        };
    }
}


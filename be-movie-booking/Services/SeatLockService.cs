using be_movie_booking.DTOs;
using be_movie_booking.Hubs;
using be_movie_booking.Repositories;
using Microsoft.AspNetCore.SignalR;

namespace be_movie_booking.Services;

public interface ISeatLockService
{
    Task<SeatLockResultDto> LockSeatsAsync(SeatLockRequestDto dto);
    Task<SeatLockResultDto> UnlockSeatsAsync(SeatUnlockRequestDto dto);
    Task<LockedSeatsResponseDto> GetLockedSeatsAsync(Guid showtimeId);
}

public class SeatLockService : ISeatLockService
{
    private readonly ISeatLockRepository _repository;
    private readonly IHubContext<AppHub> _hubContext;
    private readonly TimeSpan _seatLockDuration = TimeSpan.FromMinutes(5);

    public SeatLockService(ISeatLockRepository repository, IHubContext<AppHub> hubContext)
    {
        _repository = repository;
        _hubContext = hubContext;
    }

    public async Task<SeatLockResultDto> LockSeatsAsync(SeatLockRequestDto dto)
    {
        var userId = dto.UserId ?? Guid.Empty;
        var (lockedSeatIds, expiresAt) = await _repository.LockSeatsAsync(dto.ShowtimeId, userId, dto.SeatIds, _seatLockDuration);

        if (lockedSeatIds.Count == 0)
        {
            return new SeatLockResultDto
            {
                Success = false,
                Message = "No seats were locked.",
                ShowtimeId = dto.ShowtimeId
            };
        }

        // Broadcast thông tin tới tất cả client trong showtime có danh sách ghế đã bị khóa
        await _hubContext.Clients.Group($"showtime-{dto.ShowtimeId}")
            .SendAsync("SeatsLockUpdated", new
            {
                Action = "lock",
                LockedSeatIds = lockedSeatIds, 
                ExpiresAt = expiresAt
            });

        return new SeatLockResultDto
        {
            Success = true,
            Message = "Seats locked successfully.",
            ShowtimeId = dto.ShowtimeId,
            LockedSeatIds = lockedSeatIds,
            ExpiresAt = expiresAt
        };
    }

    public async Task<SeatLockResultDto> UnlockSeatsAsync(SeatUnlockRequestDto dto)
    {
        var userId = dto.UserId ?? Guid.Empty;
        var unlockedSeatIds = await _repository.UnlockSeatsAsync(dto.ShowtimeId, userId, dto.SeatIds);

        if (unlockedSeatIds.Count == 0)
        {
            return new SeatLockResultDto
            {
                Success = false,
                Message = "No seats were unlocked.",
                ShowtimeId = dto.ShowtimeId
            };
        }

        await _hubContext.Clients.Group($"showtime-{dto.ShowtimeId}")
            .SendAsync("SeatsLockUpdated", new
            {
                Action = "unlock",
                UnlockedSeatIds = unlockedSeatIds
            });

        return new SeatLockResultDto
        {
            Success = true,
            Message = "Seats unlocked successfully.",
            ShowtimeId = dto.ShowtimeId,
            LockedSeatIds = unlockedSeatIds
        };
    }

    public async Task<LockedSeatsResponseDto> GetLockedSeatsAsync(Guid showtimeId)
    {
        return new LockedSeatsResponseDto
        {
            ShowtimeId = showtimeId,
            LockedSeatIds = await _repository.GetLockedSeatsAsync(showtimeId)
        };
    }
}

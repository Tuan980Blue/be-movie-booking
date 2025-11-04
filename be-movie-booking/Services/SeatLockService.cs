using be_movie_booking.DTOs;
using be_movie_booking.Hubs;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using System.Text.Json;

namespace be_movie_booking.Services;

public interface ISeatLockService
{
    Task<SeatLockResultDto> LockSeatsAsync(SeatLockRequestDto dto);
    Task<SeatLockResultDto> UnlockSeatsAsync(SeatUnlockRequestDto dto);
    Task<LockedSeatsResponseDto> GetLockedSeatsAsync(Guid showtimeId);
}

public class SeatLockService : ISeatLockService
{
    private readonly IDatabase _db;
    private readonly IHubContext<AppHub> _hubContext;
    private readonly TimeSpan _seatLockDuration = TimeSpan.FromMinutes(5);

    public SeatLockService(IConnectionMultiplexer redis, IHubContext<AppHub> hubContext)
    {
        _db = redis.GetDatabase();
        _hubContext = hubContext;
    }

    // Key lưu danh sách ghế mà user đang giữ trong 1 suất chiếu
    private string GetRedisKey(Guid showtimeId, Guid userId)
        => $"seat_locks:showtime:{showtimeId}:user:{userId}";

    // Key index ngược cho từng ghế
    private string GetSeatIndexKey(Guid showtimeId, Guid seatId)
        => $"seat_index:showtime:{showtimeId}:seat:{seatId}";

    public async Task<SeatLockResultDto> LockSeatsAsync(SeatLockRequestDto dto)
    {
        var userId = dto.UserId ?? Guid.Empty;
        var redisKey = GetRedisKey(dto.ShowtimeId, userId);
        var lockedSeatIds = new List<Guid>();
        var expiresAt = DateTime.UtcNow.Add(_seatLockDuration);

        foreach (var seatId in dto.SeatIds)
        {
            // Kiểm tra ghế có đang bị người khác lock không
            var existingLockUser = await _db.StringGetAsync(GetSeatIndexKey(dto.ShowtimeId, seatId));
            if (!existingLockUser.IsNullOrEmpty) continue;

            var lockInfo = new SeatLockInfo
            {
                UserId = userId,
                LockedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt
            };

            // Lưu vào hash của user
            await _db.HashSetAsync(redisKey, seatId.ToString(), JsonSerializer.Serialize(lockInfo));

            // Lưu index ngược ghế -> userId
            await _db.StringSetAsync(GetSeatIndexKey(dto.ShowtimeId, seatId), userId.ToString(), _seatLockDuration);

            lockedSeatIds.Add(seatId);
        }

        if (lockedSeatIds.Count == 0)
        {
            return new SeatLockResultDto
            {
                Success = false,
                Message = "No seats were locked.",
                ShowtimeId = dto.ShowtimeId
            };
        }

        // Đặt TTL cho toàn bộ hash key
        await _db.KeyExpireAsync(redisKey, _seatLockDuration);

        // Broadcast thông tin tới tất cả client trong showtime
        await _hubContext.Clients.Group($"showtime-{dto.ShowtimeId}")
            .SendAsync("SeatsLocked", new
            {
                ShowtimeId = dto.ShowtimeId,
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
        var redisKey = GetRedisKey(dto.ShowtimeId, userId);
        var unlockedSeatIds = new List<Guid>();

        foreach (var seatId in dto.SeatIds)
        {
            var value = await _db.HashGetAsync(redisKey, seatId.ToString());
            if (value.IsNullOrEmpty) continue;

            var info = JsonSerializer.Deserialize<SeatLockInfo>(value!);
            if (info == null || info.UserId != userId) continue;

            // Xóa khỏi hash của user
            await _db.HashDeleteAsync(redisKey, seatId.ToString());

            // Xóa index ghế
            await _db.KeyDeleteAsync(GetSeatIndexKey(dto.ShowtimeId, seatId));

            unlockedSeatIds.Add(seatId);
        }

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
            .SendAsync("SeatsUnlocked", new
            {
                ShowtimeId = dto.ShowtimeId,
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
        var server = _db.Multiplexer.GetServer(_db.Multiplexer.GetEndPoints().First());
        var pattern = $"seat_locks:showtime:{showtimeId}:user:*";
        var lockedSeatIds = new HashSet<Guid>();

        // Duyệt qua tất cả user đang lock ghế của suất chiếu này
        foreach (var key in server.Keys(pattern: pattern))
        {
            var allLocks = await _db.HashGetAllAsync(key);
            foreach (var item in allLocks)
            {
                if (Guid.TryParse(item.Name.ToString(), out var seatId))
                {
                    lockedSeatIds.Add(seatId);
                }
            }
        }

        return new LockedSeatsResponseDto
        {
            ShowtimeId = showtimeId,
            LockedSeatIds = lockedSeatIds.ToList()
        };
    }
}

using StackExchange.Redis;
using System.Text.Json;

namespace be_movie_booking.Repositories;

public interface ISeatLockRepository
{
    Task<(List<Guid> lockedSeatIds, DateTime expiresAt)> LockSeatsAsync(Guid showtimeId, Guid userId, IEnumerable<Guid> seatIds, TimeSpan lockDuration);
    Task<List<Guid>> UnlockSeatsAsync(Guid showtimeId, Guid userId, IEnumerable<Guid> seatIds);
    Task<List<Guid>> GetLockedSeatsAsync(Guid showtimeId);
}

public class SeatLockRepository : ISeatLockRepository
{
    private readonly IDatabase _db;

    public SeatLockRepository(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    private sealed class SeatLockInfo
    {
        public Guid UserId { get; set; }
        public DateTime LockedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    private string GetRedisKey(Guid showtimeId, Guid userId)
        => $"seat_locks:showtime:{showtimeId}:user:{userId}";

    private string GetSeatIndexKey(Guid showtimeId, Guid seatId)
        => $"seat_index:showtime:{showtimeId}:seat:{seatId}";

    public async Task<(List<Guid> lockedSeatIds, DateTime expiresAt)> LockSeatsAsync(Guid showtimeId, Guid userId, IEnumerable<Guid> seatIds, TimeSpan lockDuration)
    {
        var redisKey = GetRedisKey(showtimeId, userId);
        var lockedSeatIds = new List<Guid>();
        var expiresAt = DateTime.UtcNow.Add(lockDuration);

        foreach (var seatId in seatIds)
        {
            var existingLockUser = await _db.StringGetAsync(GetSeatIndexKey(showtimeId, seatId));
            if (!existingLockUser.IsNullOrEmpty) continue;

            var lockInfo = new SeatLockInfo
            {
                UserId = userId,
                LockedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt
            };

            await _db.HashSetAsync(redisKey, seatId.ToString(), JsonSerializer.Serialize(lockInfo));
            await _db.StringSetAsync(GetSeatIndexKey(showtimeId, seatId), userId.ToString(), lockDuration);
            lockedSeatIds.Add(seatId);
        }

        if (lockedSeatIds.Count > 0)
        {
            await _db.KeyExpireAsync(redisKey, lockDuration);
        }

        return (lockedSeatIds, expiresAt);
    }

    public async Task<List<Guid>> UnlockSeatsAsync(Guid showtimeId, Guid userId, IEnumerable<Guid> seatIds)
    {
        var redisKey = GetRedisKey(showtimeId, userId);
        var unlockedSeatIds = new List<Guid>();

        foreach (var seatId in seatIds)
        {
            var value = await _db.HashGetAsync(redisKey, seatId.ToString());
            if (value.IsNullOrEmpty) continue;

            var info = JsonSerializer.Deserialize<SeatLockInfo>(value!);
            if (info == null || info.UserId != userId) continue;

            await _db.HashDeleteAsync(redisKey, seatId.ToString());
            await _db.KeyDeleteAsync(GetSeatIndexKey(showtimeId, seatId));
            unlockedSeatIds.Add(seatId);
        }

        return unlockedSeatIds;
    }

    public async Task<List<Guid>> GetLockedSeatsAsync(Guid showtimeId)
    {
        var server = _db.Multiplexer.GetServer(_db.Multiplexer.GetEndPoints().First());
        var pattern = $"seat_locks:showtime:{showtimeId}:user:*";
        var lockedSeatIds = new HashSet<Guid>();

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

        return lockedSeatIds.ToList();
    }
}
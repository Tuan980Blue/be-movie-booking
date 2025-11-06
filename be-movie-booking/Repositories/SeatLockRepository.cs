using StackExchange.Redis;
using System.Text.Json;

namespace be_movie_booking.Repositories;

public interface ISeatLockRepository
{
    Task<(List<Guid> lockedSeatIds, DateTime expiresAt)> LockSeatsAsync(Guid showtimeId, Guid userId,
        IEnumerable<Guid> seatIds, TimeSpan lockDuration);

    Task<(List<Guid> lockedSeatIds, DateTime expiresAt)> ChangeTimeLockSeatsAsync(Guid showtimeId, Guid userId,
        IEnumerable<Guid> seatIds, TimeSpan newLockDuration);

    Task<List<Guid>> UnlockSeatsAsync(Guid showtimeId, Guid userId, IEnumerable<Guid> seatIds);
    Task<List<Guid>> GetLockedSeatsAsync(Guid showtimeId);
    Task<List<Guid>> GetUserLockedSeatsAsync(Guid showtimeId, Guid userId);
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

        public Boolean IsExtended { get; set; } = false;
    }

    private string GetRedisKey(Guid showtimeId, Guid userId)
        => $"seat_locks:showtime:{showtimeId}:user:{userId}";

    private string GetSeatIndexKey(Guid showtimeId, Guid seatId)
        => $"seat_index:showtime:{showtimeId}:seat:{seatId}";

    public async Task<(List<Guid> lockedSeatIds, DateTime expiresAt)> LockSeatsAsync(Guid showtimeId, Guid userId,
        IEnumerable<Guid> seatIds, TimeSpan lockDuration)
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

    //Thay đổi thời gian khóa ghế khi người dùng đang trong quá trình thanh toán
    public async Task<(List<Guid> lockedSeatIds, DateTime expiresAt)> ChangeTimeLockSeatsAsync(
        Guid showtimeId, Guid userId, IEnumerable<Guid> seatIds, TimeSpan newLockDuration)
    {
        var redisKey = GetRedisKey(showtimeId, userId);
        var expiresAt = DateTime.UtcNow.Add(newLockDuration);
        var seatIdList = seatIds.ToList();

        // --- Bước 1: Validate tất cả ghế ---
        foreach (var seatId in seatIdList)
        {
            var seatIndexKey = GetSeatIndexKey(showtimeId, seatId);
            var existingUser = await _db.StringGetAsync(seatIndexKey);
            if (existingUser.IsNullOrEmpty || existingUser != userId.ToString())
            {
                Console.WriteLine("Ghế không bị khóa bởi người dùng này hoặc không tồn tại.");
                return (new List<Guid>(), DateTime.MinValue);
            }

            var hashValue = await _db.HashGetAsync(redisKey, seatId.ToString());
            if (hashValue.IsNullOrEmpty)
            {
                Console.WriteLine("Không tìm thấy thông tin khóa ghế trong Redis.");
                return (new List<Guid>(), DateTime.MinValue);
            }

            var info = JsonSerializer.Deserialize<SeatLockInfo>(hashValue!);
            if (info == null || info.UserId != userId)
            {
                Console.WriteLine("Thông tin khóa ghế không hợp lệ.");
                return (new List<Guid>(), DateTime.MinValue);
            }

            // Nếu ghế đã được gia hạn trước đó, không cho phép gia hạn tiếp
            if (info.IsExtended)
            {
                Console.WriteLine("Ghế đã được gia hạn trước đó, không thể gia hạn tiếp.");
                return (new List<Guid>(), DateTime.MinValue);
            }
        }

        // --- Bước 2: Tất cả hợp lệ -> Thực hiện trong 1 transaction ---
        var tran = _db.CreateTransaction();
        var lockedSeatIds = new List<Guid>();

        foreach (var seatId in seatIdList)
        {
            var info = new SeatLockInfo
            {
                UserId = userId,
                LockedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
                IsExtended = true
            };

            var seatIndexKey = GetSeatIndexKey(showtimeId, seatId);
            _ = tran.HashSetAsync(redisKey, seatId.ToString(), JsonSerializer.Serialize(info));
            _ = tran.KeyExpireAsync(seatIndexKey, newLockDuration);
            lockedSeatIds.Add(seatId);
        }

        _ = tran.KeyExpireAsync(redisKey, newLockDuration);

        var committed = await tran.ExecuteAsync();
        if (!committed)
        {
            Console.WriteLine("Transaction failed to commit.");
            return (new List<Guid>(), DateTime.MinValue);
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

    //Lấy danh sách tất cả ghế đã bị khóa cho một suất chiếu
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

    //Lấy danh sách ghế đã bị khóa bởi người dùng cụ thể cho một suất chiếu
    public async Task<List<Guid>> GetUserLockedSeatsAsync(Guid showtimeId, Guid userId)
    {
        var redisKey = GetRedisKey(showtimeId, userId);
        var lockedSeatIds = new List<Guid>();

        var allLocks = await _db.HashGetAllAsync(redisKey);
        foreach (var item in allLocks)
        {
            if (Guid.TryParse(item.Name.ToString(), out var seatId))
            {
                var lockInfo = JsonSerializer.Deserialize<SeatLockInfo>(item.Value!);
                if (lockInfo != null && lockInfo.UserId == userId && lockInfo.ExpiresAt > DateTime.UtcNow)
                {
                    lockedSeatIds.Add(seatId);
                }
            }
        }

        return lockedSeatIds;
    }
}
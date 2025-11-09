using StackExchange.Redis;
using System.Text.Json;
using be_movie_booking.DTOs;

namespace be_movie_booking.Repositories;

/// <summary>
/// Repository để lưu draft booking vào Redis (chưa thanh toán)
/// </summary>
public interface IBookingDraftRepository
{
    Task<BookingDraftDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task SaveAsync(BookingDraftDto draft, TimeSpan? ttl = null, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExtendTtlAsync(Guid id, TimeSpan ttl, CancellationToken ct = default);
}

public class BookingDraftRepository : IBookingDraftRepository
{
    private readonly IDatabase _db;
    private readonly TimeSpan _defaultTtl = TimeSpan.FromMinutes(3);  //TTL cùng đồng bộ với SeatLock

    public BookingDraftRepository(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    private string GetRedisKey(Guid id) => $"booking_draft:{id}";

    public async Task<BookingDraftDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var key = GetRedisKey(id);
        var value = await _db.StringGetAsync(key);
        
        if (value.IsNullOrEmpty)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<BookingDraftDto>(value!);
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveAsync(BookingDraftDto draft, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        var key = GetRedisKey(draft.Id);
        var ttlToUse = ttl ?? _defaultTtl;

        var draftJson = JsonSerializer.Serialize(draft);
        
        // Lưu draft với TTL
        await _db.StringSetAsync(key, draftJson, ttlToUse);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var key = GetRedisKey(id);
        return await _db.KeyDeleteAsync(key);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        var key = GetRedisKey(id);
        return await _db.KeyExistsAsync(key);
    }

    /// <summary>
    /// Extends the TTL of an existing draft booking
    /// </summary>
    public async Task<bool> ExtendTtlAsync(Guid id, TimeSpan ttl, CancellationToken ct = default)
    {
        var key = GetRedisKey(id);
        // Check if key exists first
        if (!await _db.KeyExistsAsync(key))
        {
            return false;
        }
        // Extend TTL
        return await _db.KeyExpireAsync(key, ttl);
    }
}


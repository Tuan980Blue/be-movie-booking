using be_movie_booking.Data;
using be_movie_booking.Models;
using Microsoft.EntityFrameworkCore;

namespace be_movie_booking.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);
    Task AddAsync(RefreshToken token, CancellationToken ct = default);
    Task<List<RefreshToken>> GetActiveByUserAsync(Guid userId, CancellationToken ct = default);
}
public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly MovieBookingDbContext _db;

    public RefreshTokenRepository(MovieBookingDbContext db)
    {
        _db = db;
    }

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default)
    {
        return _db.RefreshTokens.Include(r => r.User).FirstOrDefaultAsync(r => r.TokenHash == tokenHash, ct);
    }

    public async Task AddAsync(RefreshToken token, CancellationToken ct = default)
    {
        await _db.RefreshTokens.AddAsync(token, ct);
    }

    public Task<List<RefreshToken>> GetActiveByUserAsync(Guid userId, CancellationToken ct = default)
    {
        return _db.RefreshTokens.Where(r => r.UserId == userId && r.RevokedAt == null && r.ExpiresAt > DateTime.UtcNow).ToListAsync(ct);
    }
}

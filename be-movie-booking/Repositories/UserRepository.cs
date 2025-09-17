using be_movie_booking.Data;
using be_movie_booking.Models;
using Microsoft.EntityFrameworkCore;

namespace be_movie_booking.Repositories;

public interface IUserRepository
{
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
}
public class UserRepository : IUserRepository
{
    private readonly MovieBookingDbContext _db;

    public UserRepository(MovieBookingDbContext db)
    {
        _db = db;
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
    {
        return _db.Users.AnyAsync(u => u.Email == email, ct);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        return _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
    }

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        await _db.Users.AddAsync(user, ct);
    }
}

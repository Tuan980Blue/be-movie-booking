using be_movie_booking.Data;
using be_movie_booking.Models;
using Microsoft.EntityFrameworkCore;

namespace be_movie_booking.Repositories;

/// <summary>
/// Interface cho Genre Repository
/// </summary>
public interface IGenreRepository
{
    Task<Genre?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Genre>> ListAsync(CancellationToken ct = default);
    Task<Genre?> AddAsync(Genre genre, CancellationToken ct = default);
    Task<Genre?> UpdateAsync(Genre genre, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);
    Task<List<Genre>> GetByIdsAsync(List<Guid> ids, CancellationToken ct = default);
    Task<Genre?> GetByIdWithMoviesAsync(Guid id, CancellationToken ct = default);
}

/// <summary>
/// Repository để xử lý data access cho Genre
/// </summary>
public class GenreRepository : IGenreRepository
{
    private readonly MovieBookingDbContext _db;

    public GenreRepository(MovieBookingDbContext db)
    {
        _db = db;
    }

    public async Task<Genre?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Genres
            .FirstOrDefaultAsync(g => g.Id == id, ct);
    }

    public async Task<List<Genre>> ListAsync(CancellationToken ct = default)
    {
        return await _db.Genres
            .Include(g => g.MovieGenres)
            .OrderBy(g => g.Name)
            .ToListAsync(ct);
    }

    public async Task<Genre?> AddAsync(Genre genre, CancellationToken ct = default)
    {
        await _db.Genres.AddAsync(genre, ct);
        await _db.SaveChangesAsync(ct);
        return genre;
    }

    public async Task<Genre?> UpdateAsync(Genre genre, CancellationToken ct = default)
    {
        _db.Genres.Update(genre);
        await _db.SaveChangesAsync(ct);
        return genre;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var genre = await _db.Genres.FindAsync(id);
        if (genre == null) return false;

        _db.Genres.Remove(genre);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Genres.AnyAsync(g => g.Id == id, ct);
    }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default)
    {
        return await _db.Genres.AnyAsync(g => g.Name.ToLower() == name.ToLower(), ct);
    }

    public async Task<List<Genre>> GetByIdsAsync(List<Guid> ids, CancellationToken ct = default)
    {
        return await _db.Genres
            .Where(g => ids.Contains(g.Id))
            .ToListAsync(ct);
    }

    public async Task<Genre?> GetByIdWithMoviesAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Genres
            .Include(g => g.MovieGenres)
            .FirstOrDefaultAsync(g => g.Id == id, ct);
    }
}

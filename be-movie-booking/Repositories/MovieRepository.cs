using be_movie_booking.Data;
using be_movie_booking.DTOs;
using be_movie_booking.Models;
using Microsoft.EntityFrameworkCore;

namespace be_movie_booking.Repositories;

/// <summary>
/// Interface cho Movie Repository
/// </summary>
public interface IMovieRepository
{
    Task<Movie?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Movie?> GetByIdWithGenresAsync(Guid id, CancellationToken ct = default);
    Task<(List<Movie> movies, int total)> ListAsync(MovieSearchDto searchDto, CancellationToken ct = default);
    Task<Movie?> AddAsync(Movie movie, CancellationToken ct = default);
    Task<Movie?> UpdateAsync(Movie movie, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
    Task<List<Movie>> GetByStatusAsync(string status, CancellationToken ct = default);
    Task<MovieStatsDto> GetStatsAsync(CancellationToken ct = default);
    Task<bool> ExistsByTitleAsync(string title, CancellationToken ct = default);
    Task<bool> ExistsByTitleAsync(string title, Guid excludeId, CancellationToken ct = default);
}

/// <summary>
/// Repository để xử lý data access cho Movie
/// </summary>
public class MovieRepository : IMovieRepository
{
    private readonly MovieBookingDbContext _db;

    public MovieRepository(MovieBookingDbContext db)
    {
        _db = db;
    }

    public async Task<Movie?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Movies
            .FirstOrDefaultAsync(m => m.Id == id, ct);
    }

    public async Task<Movie?> GetByIdWithGenresAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Movies
            .Include(m => m.MovieGenres)
            .ThenInclude(mg => mg.Genre)
            .FirstOrDefaultAsync(m => m.Id == id, ct);
    }

    public async Task<(List<Movie> movies, int total)> ListAsync(MovieSearchDto searchDto, CancellationToken ct = default)
    {
        var query = _db.Movies
            .Include(m => m.MovieGenres)
            .ThenInclude(mg => mg.Genre)
            .AsQueryable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(searchDto.Search))
        {
            var searchTerm = searchDto.Search.Trim().ToLower();
            query = query.Where(m => 
                m.Title.ToLower().Contains(searchTerm) ||
                (m.OriginalTitle != null && m.OriginalTitle.ToLower().Contains(searchTerm)) ||
                (m.Description != null && m.Description.ToLower().Contains(searchTerm)) ||
                (m.Director != null && m.Director.ToLower().Contains(searchTerm)) ||
                (m.Actors != null && m.Actors.ToLower().Contains(searchTerm)));
        }

        // Apply genre filter
        if (searchDto.GenreIds != null && searchDto.GenreIds.Any())
        {
            query = query.Where(m => m.MovieGenres.Any(mg => searchDto.GenreIds.Contains(mg.GenreId)));
        }

        // Apply status filter
        if (searchDto.Status.HasValue)
        {
            query = query.Where(m => m.Status == searchDto.Status.Value);
        }

        // Apply release year filter
        if (searchDto.ReleaseYear.HasValue)
        {
            var year = searchDto.ReleaseYear.Value;
            query = query.Where(m => m.ReleaseDate.HasValue && m.ReleaseDate.Value.Year == year);
        }

        // Apply release date range filter
        if (searchDto.ReleaseDateFrom.HasValue)
        {
            query = query.Where(m => m.ReleaseDate >= searchDto.ReleaseDateFrom.Value);
        }

        if (searchDto.ReleaseDateTo.HasValue)
        {
            query = query.Where(m => m.ReleaseDate <= searchDto.ReleaseDateTo.Value);
        }

        // Get total count before pagination
        var total = await query.CountAsync(ct);

        // Apply sorting
        query = searchDto.SortBy.ToLower() switch
        {
            "title" => searchDto.SortDirection.ToLower() == "asc" 
                ? query.OrderBy(m => m.Title) 
                : query.OrderByDescending(m => m.Title),
            "releasedate" => searchDto.SortDirection.ToLower() == "asc" 
                ? query.OrderBy(m => m.ReleaseDate) 
                : query.OrderByDescending(m => m.ReleaseDate),
            "duration" => searchDto.SortDirection.ToLower() == "asc" 
                ? query.OrderBy(m => m.DurationMinutes) 
                : query.OrderByDescending(m => m.DurationMinutes),
            "status" => searchDto.SortDirection.ToLower() == "asc" 
                ? query.OrderBy(m => m.Status) 
                : query.OrderByDescending(m => m.Status),
            "director" => searchDto.SortDirection.ToLower() == "asc" 
                ? query.OrderBy(m => m.Director) 
                : query.OrderByDescending(m => m.Director),
            "actors" => searchDto.SortDirection.ToLower() == "asc" 
                ? query.OrderBy(m => m.Actors) 
                : query.OrderByDescending(m => m.Actors),
            _ => searchDto.SortDirection.ToLower() == "asc" 
                ? query.OrderBy(m => m.CreatedAt) 
                : query.OrderByDescending(m => m.CreatedAt)
        };

        // Apply pagination
        var movies = await query
            .Skip((searchDto.Page - 1) * searchDto.PageSize)
            .Take(searchDto.PageSize)
            .ToListAsync(ct);

        return (movies, total);
    }

    public async Task<Movie?> AddAsync(Movie movie, CancellationToken ct = default)
    {
        await _db.Movies.AddAsync(movie, ct);
        await _db.SaveChangesAsync(ct);
        return movie;
    }

    public async Task<Movie?> UpdateAsync(Movie movie, CancellationToken ct = default)
    {
        _db.Movies.Update(movie);
        await _db.SaveChangesAsync(ct);
        return movie;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var movie = await _db.Movies.FindAsync(id);
        if (movie == null) return false;

        _db.Movies.Remove(movie);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Movies.AnyAsync(m => m.Id == id, ct);
    }

    public async Task<List<Movie>> GetByStatusAsync(string status, CancellationToken ct = default)
    {
        if (Enum.TryParse<MovieStatus>(status, true, out var movieStatus)) // Case-insensitive
        {
            return await _db.Movies
                .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
                .Where(m => m.Status == movieStatus)
                .OrderBy(m => m.Title) // Thêm sorting
                .ToListAsync(ct);
        }
        return new List<Movie>();
    }

    public async Task<MovieStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        var stats = await _db.Movies
            .GroupBy(m => m.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var totalMovies = await _db.Movies.CountAsync(ct);

        return new MovieStatsDto
        {
            TotalMovies = totalMovies,
            DraftMovies = stats.FirstOrDefault(s => s.Status == MovieStatus.Draft)?.Count ?? 0,
            ComingSoonMovies = stats.FirstOrDefault(s => s.Status == MovieStatus.ComingSoon)?.Count ?? 0,
            NowShowingMovies = stats.FirstOrDefault(s => s.Status == MovieStatus.NowShowing)?.Count ?? 0,
            ArchivedMovies = stats.FirstOrDefault(s => s.Status == MovieStatus.Archived)?.Count ?? 0
        };
    }

    public async Task<bool> ExistsByTitleAsync(string title, CancellationToken ct = default)
    {
        return await _db.Movies.AnyAsync(m => m.Title.ToLower() == title.ToLower(), ct);
    }

    public async Task<bool> ExistsByTitleAsync(string title, Guid excludeId, CancellationToken ct = default)
    {
        return await _db.Movies.AnyAsync(m => m.Title.ToLower() == title.ToLower() && m.Id != excludeId, ct);
    }
}

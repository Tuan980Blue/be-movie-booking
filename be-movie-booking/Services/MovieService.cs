using be_movie_booking.DTOs;
using be_movie_booking.Models;
using be_movie_booking.Repositories;

namespace be_movie_booking.Services;

/// <summary>
/// Interface cho Movie Service
/// </summary>
public interface IMovieService
{
    Task<MovieReadDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<MovieReadDto>> ListAsync(MovieSearchDto searchDto, CancellationToken ct = default);
    Task<MovieReadDto?> CreateAsync(CreateMovieDto dto, CancellationToken ct = default);
    Task<MovieReadDto?> UpdateAsync(Guid id, UpdateMovieDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<MovieReadDto?> ChangeStatusAsync(Guid id, ChangeMovieStatusDto dto, CancellationToken ct = default);
    Task<List<MovieReadDto>> GetByStatusAsync(string status, CancellationToken ct = default);
    Task<MovieStatsDto> GetStatsAsync(CancellationToken ct = default);
}

/// <summary>
/// Service để xử lý business logic cho Movie
/// </summary>
public class MovieService : IMovieService
{
    private readonly IMovieRepository _movieRepository;
    private readonly IGenreRepository _genreRepository;

    public MovieService(IMovieRepository movieRepository, IGenreRepository genreRepository)
    {
        _movieRepository = movieRepository;
        _genreRepository = genreRepository;
    }

    public async Task<MovieReadDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var movie = await _movieRepository.GetByIdWithGenresAsync(id, ct);
        return movie == null ? null : MapToReadDto(movie);
    }

    public async Task<PagedResult<MovieReadDto>> ListAsync(MovieSearchDto searchDto, CancellationToken ct = default)
    {
        var (movies, total) = await _movieRepository.ListAsync(searchDto, ct);
        var items = movies.Select(MapToReadDto).ToList();

        return new PagedResult<MovieReadDto>
        {
            Items = items,
            Page = searchDto.Page,
            PageSize = searchDto.PageSize,
            TotalItems = total
        };
    }

    public async Task<MovieReadDto?> CreateAsync(CreateMovieDto dto, CancellationToken ct = default)
    {
        // Validate genres exist
        var genres = await _genreRepository.GetByIdsAsync(dto.GenreIds, ct);
        if (genres.Count != dto.GenreIds.Count)
        {
            throw new ArgumentException("Một hoặc nhiều thể loại không tồn tại");
        }

        // Check if movie title already exists
        var titleExists = await _movieRepository.ExistsByTitleAsync(dto.Title, ct);
        if (titleExists)
        {
            throw new ArgumentException($"Phim với tên '{dto.Title}' đã tồn tại");
        }

        var movie = new Movie
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            OriginalTitle = dto.OriginalTitle,
            DurationMinutes = dto.DurationMinutes,
            Rated = dto.Rated,
            Description = dto.Description,
            ReleaseDate = dto.ReleaseDate,
            PosterUrl = dto.PosterUrl,
            BackdropUrl = dto.BackdropUrl,
            TrailerUrl = dto.TrailerUrl,
            Director = dto.Director,
            Actors = dto.Actors,
            Status = MovieStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        // Add movie genres
        foreach (var genreId in dto.GenreIds)
        {
            movie.MovieGenres.Add(new MovieGenre
            {
                MovieId = movie.Id,
                GenreId = genreId
            });
        }

        var createdMovie = await _movieRepository.AddAsync(movie, ct);
        return createdMovie == null ? null : MapToReadDto(createdMovie);
    }

    public async Task<MovieReadDto?> UpdateAsync(Guid id, UpdateMovieDto dto, CancellationToken ct = default)
    {
        var movie = await _movieRepository.GetByIdWithGenresAsync(id, ct);
        if (movie == null) return null;

        // Validate genres exist
        var genres = await _genreRepository.GetByIdsAsync(dto.GenreIds, ct);
        if (genres.Count != dto.GenreIds.Count)
        {
            throw new ArgumentException("Một hoặc nhiều thể loại không tồn tại");
        }

        // Check if movie title already exists (excluding current movie)
        var titleExists = await _movieRepository.ExistsByTitleAsync(dto.Title, id, ct);
        if (titleExists)
        {
            throw new ArgumentException($"Phim với tên '{dto.Title}' đã tồn tại");
        }

        // Update movie properties
        movie.Title = dto.Title;
        movie.OriginalTitle = dto.OriginalTitle;
        movie.DurationMinutes = dto.DurationMinutes;
        movie.Rated = dto.Rated;
        movie.Description = dto.Description;
        movie.ReleaseDate = dto.ReleaseDate;
        movie.PosterUrl = dto.PosterUrl;
        movie.BackdropUrl = dto.BackdropUrl;
        movie.TrailerUrl = dto.TrailerUrl;
        movie.Director = dto.Director;
        movie.Actors = dto.Actors;
        movie.UpdatedAt = DateTime.UtcNow;

        // Update genres
        movie.MovieGenres.Clear();
        foreach (var genreId in dto.GenreIds)
        {
            movie.MovieGenres.Add(new MovieGenre
            {
                MovieId = movie.Id,
                GenreId = genreId
            });
        }

        var updatedMovie = await _movieRepository.UpdateAsync(movie, ct);
        return updatedMovie == null ? null : MapToReadDto(updatedMovie);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        return await _movieRepository.DeleteAsync(id, ct);
    }

    public async Task<MovieReadDto?> ChangeStatusAsync(Guid id, ChangeMovieStatusDto dto, CancellationToken ct = default)
    {
        var movie = await _movieRepository.GetByIdWithGenresAsync(id, ct);
        if (movie == null) return null;

        if (Enum.TryParse<MovieStatus>(dto.Status, out var newStatus))
        {
            movie.Status = newStatus;
            var updatedMovie = await _movieRepository.UpdateAsync(movie, ct);
            return updatedMovie == null ? null : MapToReadDto(updatedMovie);
        }

        throw new ArgumentException("Trạng thái không hợp lệ");
    }

    public async Task<List<MovieReadDto>> GetByStatusAsync(string status, CancellationToken ct = default)
    {
        // Validate status parameter
        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException("Trạng thái không được để trống");
        }

        // Check if status is valid
        if (!Enum.TryParse<MovieStatus>(status, true, out var movieStatus))
        {
            var validStatuses = string.Join(", ", Enum.GetNames<MovieStatus>());
            throw new ArgumentException($"Trạng thái không hợp lệ. Các trạng thái hợp lệ: {validStatuses}");
        }

        var movies = await _movieRepository.GetByStatusAsync(status, ct);
        return movies.Select(MapToReadDto).ToList();
    }

    public async Task<MovieStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        return await _movieRepository.GetStatsAsync(ct);
    }

    /// <summary>
    /// Map Movie entity to MovieReadDto
    /// </summary>
    private static MovieReadDto MapToReadDto(Movie movie)
    {
        return new MovieReadDto
        {
            Id = movie.Id,
            Title = movie.Title,
            OriginalTitle = movie.OriginalTitle,
            DurationMinutes = movie.DurationMinutes,
            Rated = movie.Rated,
            Description = movie.Description,
            ReleaseDate = movie.ReleaseDate,
            PosterUrl = movie.PosterUrl,
            BackdropUrl = movie.BackdropUrl,
            TrailerUrl = movie.TrailerUrl,
            Director = movie.Director,
            Actors = movie.Actors,
            Status = movie.Status.ToString(),
            CreatedAt = movie.CreatedAt,
            UpdatedAt = movie.UpdatedAt,
            Genres = movie.MovieGenres.Select(mg => new GenreReadDto
            {
                Id = mg.Genre.Id,
                Name = mg.Genre.Name
            }).ToList()
        };
    }
}

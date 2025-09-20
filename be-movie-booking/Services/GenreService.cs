using be_movie_booking.DTOs;
using be_movie_booking.Models;
using be_movie_booking.Repositories;

namespace be_movie_booking.Services;

/// <summary>
/// Interface cho Genre Service
/// </summary>
public interface IGenreService
{
    Task<List<GenreReadDto>> ListAsync(CancellationToken ct = default);
    Task<GenreReadDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<GenreReadDto?> CreateAsync(CreateGenreDto dto, CancellationToken ct = default);
    Task<GenreReadDto?> UpdateAsync(Guid id, UpdateGenreDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}

/// <summary>
/// Service để xử lý business logic cho Genre
/// </summary>
public class GenreService : IGenreService
{
    private readonly IGenreRepository _genreRepository;

    public GenreService(IGenreRepository genreRepository)
    {
        _genreRepository = genreRepository;
    }

    public async Task<List<GenreReadDto>> ListAsync(CancellationToken ct = default)
    {
        var genres = await _genreRepository.ListAsync(ct);
        return genres.Select(MapToReadDto).ToList();
    }

    public async Task<GenreReadDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var genre = await _genreRepository.GetByIdAsync(id, ct);
        return genre == null ? null : MapToReadDto(genre);
    }

    public async Task<GenreReadDto?> CreateAsync(CreateGenreDto dto, CancellationToken ct = default)
    {
        // Check if genre name already exists
        var exists = await _genreRepository.ExistsByNameAsync(dto.Name, ct);
        if (exists)
        {
            throw new ArgumentException("Tên thể loại đã tồn tại");
        }

        var genre = new Genre
        {
            Id = Guid.NewGuid(),
            Name = dto.Name
        };

        var createdGenre = await _genreRepository.AddAsync(genre, ct);
        return createdGenre == null ? null : MapToReadDto(createdGenre);
    }

    public async Task<GenreReadDto?> UpdateAsync(Guid id, UpdateGenreDto dto, CancellationToken ct = default)
    {
        var genre = await _genreRepository.GetByIdAsync(id, ct);
        if (genre == null) return null;

        // Check if genre name already exists (excluding current genre)
        var exists = await _genreRepository.ExistsByNameAsync(dto.Name, ct);
        if (exists)
        {
            var existingGenre = await _genreRepository.GetByIdAsync(id, ct);
            if (existingGenre?.Name.ToLower() != dto.Name.ToLower())
            {
                throw new ArgumentException("Tên thể loại đã tồn tại");
            }
        }

        genre.Name = dto.Name;
        var updatedGenre = await _genreRepository.UpdateAsync(genre, ct);
        return updatedGenre == null ? null : MapToReadDto(updatedGenre);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        return await _genreRepository.DeleteAsync(id, ct);
    }

    /// <summary>
    /// Map Genre entity to GenreReadDto
    /// </summary>
    private static GenreReadDto MapToReadDto(Genre genre)
    {
        return new GenreReadDto
        {
            Id = genre.Id,
            Name = genre.Name
        };
    }
}

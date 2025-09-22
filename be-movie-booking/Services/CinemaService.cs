using be_movie_booking.DTOs;
using be_movie_booking.Models;
using be_movie_booking.Repositories;

namespace be_movie_booking.Services;

/// <summary>
/// Interface cho Cinema Service
/// </summary>
public interface ICinemaService
{
    Task<CinemaReadDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<CinemaListDto>> ListAsync(CinemaSearchDto searchDto, CancellationToken ct = default);
    Task<CinemaReadDto?> CreateAsync(CreateCinemaDto dto, CancellationToken ct = default);
    Task<CinemaReadDto?> UpdateAsync(Guid id, UpdateCinemaDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<CinemaReadDto?> ChangeStatusAsync(Guid id, ChangeCinemaStatusDto dto, CancellationToken ct = default);
    Task<CinemaStatsDto> GetStatsAsync(Guid id, CancellationToken ct = default);
}

/// <summary>
/// Service để xử lý business logic cho Cinema
/// </summary>
public class CinemaService : ICinemaService
{
    private readonly ICinemaRepository _cinemaRepository;

    public CinemaService(ICinemaRepository cinemaRepository)
    {
        _cinemaRepository = cinemaRepository;
    }

    public async Task<CinemaReadDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var cinema = await _cinemaRepository.GetByIdWithRoomsAsync(id, ct);
        return cinema == null ? null : MapToReadDto(cinema);
    }

    public async Task<PagedResult<CinemaListDto>> ListAsync(CinemaSearchDto searchDto, CancellationToken ct = default)
    {
        var (cinemas, total) = await _cinemaRepository.ListAsync(searchDto, ct);
        var items = cinemas.Select(MapToListDto).ToList();

        return new PagedResult<CinemaListDto>
        {
            Items = items,
            Page = searchDto.Page,
            PageSize = searchDto.PageSize,
            TotalItems = total
        };
    }

    public async Task<CinemaReadDto?> CreateAsync(CreateCinemaDto dto, CancellationToken ct = default)
    {
        // Check if cinema name already exists
        var nameExists = await _cinemaRepository.ExistsByNameAsync(dto.Name, ct);
        if (nameExists)
        {
            throw new ArgumentException($"Rạp chiếu phim với tên '{dto.Name}' đã tồn tại");
        }

        var cinema = new Cinema
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Address = dto.Address,
            City = dto.City,
            Lat = dto.Lat,
            Lng = dto.Lng,
            Status = EntityStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        var createdCinema = await _cinemaRepository.AddAsync(cinema, ct);
        return createdCinema == null ? null : MapToReadDto(createdCinema);
    }

    public async Task<CinemaReadDto?> UpdateAsync(Guid id, UpdateCinemaDto dto, CancellationToken ct = default)
    {
        var cinema = await _cinemaRepository.GetByIdWithRoomsAsync(id, ct);
        if (cinema == null) return null;

        // Check if cinema name already exists (excluding current cinema)
        var nameExists = await _cinemaRepository.ExistsByNameAsync(dto.Name, id, ct);
        if (nameExists)
        {
            throw new ArgumentException($"Rạp chiếu phim với tên '{dto.Name}' đã tồn tại");
        }

        // Update cinema properties
        cinema.Name = dto.Name;
        cinema.Address = dto.Address;
        cinema.City = dto.City;
        cinema.Lat = dto.Lat;
        cinema.Lng = dto.Lng;

        var updatedCinema = await _cinemaRepository.UpdateAsync(cinema, ct);
        return updatedCinema == null ? null : MapToReadDto(updatedCinema);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        // Check if cinema has active rooms
        var hasActiveRooms = await _cinemaRepository.HasActiveRoomsAsync(id, ct);
        if (hasActiveRooms)
        {
            throw new InvalidOperationException("Không thể xóa rạp chiếu phim có phòng chiếu đang hoạt động");
        }

        return await _cinemaRepository.DeleteAsync(id, ct);
    }

    public async Task<CinemaReadDto?> ChangeStatusAsync(Guid id, ChangeCinemaStatusDto dto, CancellationToken ct = default)
    {
        var cinema = await _cinemaRepository.GetByIdWithRoomsAsync(id, ct);
        if (cinema == null) return null;

        if (Enum.TryParse<EntityStatus>(dto.Status, out var newStatus))
        {
            cinema.Status = newStatus;
            var updatedCinema = await _cinemaRepository.UpdateAsync(cinema, ct);
            return updatedCinema == null ? null : MapToReadDto(updatedCinema);
        }

        throw new ArgumentException("Trạng thái không hợp lệ");
    }

    public async Task<CinemaStatsDto> GetStatsAsync(Guid id, CancellationToken ct = default)
    {
        return await _cinemaRepository.GetStatsAsync(id, ct);
    }

    /// <summary>
    /// Map Cinema entity to CinemaReadDto
    /// </summary>
    private static CinemaReadDto MapToReadDto(Cinema cinema)
    {
        return new CinemaReadDto
        {
            Id = cinema.Id,
            Name = cinema.Name,
            Address = cinema.Address,
            City = cinema.City,
            Lat = cinema.Lat,
            Lng = cinema.Lng,
            Status = cinema.Status.ToString(),
            CreatedAt = cinema.CreatedAt,
            TotalRooms = cinema.Rooms.Count,
            ActiveRooms = cinema.Rooms.Count(r => r.Status == EntityStatus.Active)
        };
    }

    /// <summary>
    /// Map Cinema entity to CinemaListDto
    /// </summary>
    private static CinemaListDto MapToListDto(Cinema cinema)
    {
        return new CinemaListDto
        {
            Id = cinema.Id,
            Name = cinema.Name,
            Address = cinema.Address,
            City = cinema.City,
            Status = cinema.Status.ToString(),
            CreatedAt = cinema.CreatedAt,
            TotalRooms = cinema.Rooms.Count
        };
    }
}

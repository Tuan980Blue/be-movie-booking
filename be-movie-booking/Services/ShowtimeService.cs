using be_movie_booking.DTOs;
using be_movie_booking.Models;
using be_movie_booking.Repositories;

namespace be_movie_booking.Services;

/// <summary>
/// Interface cho Showtime Service - CHỈ CÁC CHỨC NĂNG CỐT LÕI
/// </summary>
public interface IShowtimeService
{
    Task<ShowtimeReadDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<ShowtimeReadDto>> ListAsync(ShowtimeSearchDto searchDto, CancellationToken ct = default);
    Task<ShowtimeReadDto?> CreateAsync(CreateShowtimeDto dto, CancellationToken ct = default);
    Task<ShowtimeReadDto?> UpdateAsync(Guid id, UpdateShowtimeDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}

/// <summary>
/// Service để xử lý business logic cho Showtime
/// </summary>
public class ShowtimeService : IShowtimeService
{
    private readonly IShowtimeRepository _showtimeRepository;
    private readonly IMovieRepository _movieRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly ISeatRepository _seatRepository;
    private readonly IPriceRuleService _priceRuleService;

    public ShowtimeService(
        IShowtimeRepository showtimeRepository,
        IMovieRepository movieRepository,
        IRoomRepository roomRepository,
        ISeatRepository seatRepository,
        IPriceRuleService priceRuleService)
    {
        _showtimeRepository = showtimeRepository;
        _movieRepository = movieRepository;
        _roomRepository = roomRepository;
        _seatRepository = seatRepository;
        _priceRuleService = priceRuleService;
    }

    public async Task<ShowtimeReadDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var showtime = await _showtimeRepository.GetByIdWithDetailsAsync(id, ct);
        if (showtime == null) return null;
        return MapToReadDto(showtime);
    }

    public async Task<PagedResult<ShowtimeReadDto>> ListAsync(ShowtimeSearchDto searchDto, CancellationToken ct = default)
    {
        var (showtimes, total) = await _showtimeRepository.ListAsync(searchDto, ct);
        var items = new List<ShowtimeReadDto>();

        foreach (var showtime in showtimes)
        {
            items.Add(MapToReadDto(showtime));
        }

        return new PagedResult<ShowtimeReadDto>
        {
            Items = items,
            Page = searchDto.Page,
            PageSize = searchDto.PageSize,
            TotalItems = total
        };
    }


    public async Task<ShowtimeReadDto?> CreateAsync(CreateShowtimeDto dto, CancellationToken ct = default)
    {
        // Validate movie exists
        var movie = await _movieRepository.GetByIdAsync(dto.MovieId, ct);
        if (movie == null)
            throw new ArgumentException("Movie not found");

        // Validate room exists
        var room = await _roomRepository.GetByIdAsync(dto.RoomId, ct);
        if (room == null)
            throw new ArgumentException("Room not found");

        // Check for time conflicts
        var hasConflict = await _showtimeRepository.HasTimeConflictAsync(dto.RoomId, dto.StartUtc, dto.EndUtc, null, ct);
        if (hasConflict)
            throw new InvalidOperationException("Time conflict with existing showtime");

        // Validate time logic
        if (dto.EndUtc <= dto.StartUtc)
            throw new ArgumentException("End time must be after start time");

        // Validate format
        if (!Enum.TryParse<MovieFormat>(dto.Format, true, out var format))
            throw new ArgumentException("Invalid movie format");

        var showtime = new Showtime
        {
            Id = Guid.NewGuid(),
            MovieId = dto.MovieId,
            RoomId = dto.RoomId,
            StartUtc = dto.StartUtc,
            EndUtc = dto.EndUtc,
            Language = dto.Language,
            Subtitle = dto.Subtitle,
            Format = format,
            BasePriceMinor = dto.BasePriceMinor,
            CreatedAt = DateTime.UtcNow
        };

        var createdShowtime = await _showtimeRepository.AddAsync(showtime, ct);
        if (createdShowtime == null) return null;

        // Get the created showtime with details
        var showtimeWithDetails = await _showtimeRepository.GetByIdWithDetailsAsync(createdShowtime.Id, ct);
        if (showtimeWithDetails == null) return null;
        return MapToReadDto(showtimeWithDetails);
    }

    public async Task<ShowtimeReadDto?> UpdateAsync(Guid id, UpdateShowtimeDto dto, CancellationToken ct = default)
    {
        var showtime = await _showtimeRepository.GetByIdAsync(id, ct);
        if (showtime == null) return null;

        // Validate movie exists
        var movie = await _movieRepository.GetByIdAsync(dto.MovieId, ct);
        if (movie == null)
            throw new ArgumentException("Movie not found");

        // Validate room exists
        var room = await _roomRepository.GetByIdAsync(dto.RoomId, ct);
        if (room == null)
            throw new ArgumentException("Room not found");

        // Check for time conflicts (excluding current showtime)
        var hasConflict = await _showtimeRepository.HasTimeConflictAsync(dto.RoomId, dto.StartUtc, dto.EndUtc, id, ct);
        if (hasConflict)
            throw new InvalidOperationException("Time conflict with existing showtime");

        // Validate time logic
        if (dto.EndUtc <= dto.StartUtc)
            throw new ArgumentException("End time must be after start time");

        // Validate format
        if (!Enum.TryParse<MovieFormat>(dto.Format, true, out var format))
            throw new ArgumentException("Invalid movie format");

        // Update showtime
        showtime.MovieId = dto.MovieId;
        showtime.RoomId = dto.RoomId;
        showtime.StartUtc = dto.StartUtc;
        showtime.EndUtc = dto.EndUtc;
        showtime.Language = dto.Language;
        showtime.Subtitle = dto.Subtitle;
        showtime.Format = format;
        showtime.BasePriceMinor = dto.BasePriceMinor;

        var updatedShowtime = await _showtimeRepository.UpdateAsync(showtime, ct);
        if (updatedShowtime == null) return null;

        // Get the updated showtime with details
        var showtimeWithDetails = await _showtimeRepository.GetByIdWithDetailsAsync(updatedShowtime.Id, ct);
        if (showtimeWithDetails == null) return null;
        return MapToReadDto(showtimeWithDetails);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var exists = await _showtimeRepository.ExistsAsync(id, ct);
        if (!exists) return false;
        return await _showtimeRepository.DeleteAsync(id, ct);
    }


    private static ShowtimeReadDto MapToReadDto(Showtime showtime)
    {
        return new ShowtimeReadDto
        {
            Id = showtime.Id,
            MovieId = showtime.MovieId,
            MovieTitle = showtime.Movie.Title,
            MovieDurationMinutes = showtime.Movie.DurationMinutes,
            MoviePosterUrl = showtime.Movie.PosterUrl,
            RoomId = showtime.RoomId,
            RoomName = showtime.Room.Name,
            RoomCode = showtime.Room.Code,
            CinemaId = showtime.Room.CinemaId,
            CinemaName = showtime.Room.Cinema.Name,
            CinemaAddress = showtime.Room.Cinema.Address,
            StartUtc = showtime.StartUtc,
            EndUtc = showtime.EndUtc,
            Language = showtime.Language,
            Subtitle = showtime.Subtitle,
            Format = showtime.Format.ToString(),
            BasePriceMinor = showtime.BasePriceMinor,
            CreatedAt = showtime.CreatedAt
        };
    }

}

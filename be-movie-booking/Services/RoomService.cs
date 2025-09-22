using be_movie_booking.DTOs;
using be_movie_booking.Models;
using be_movie_booking.Repositories;

namespace be_movie_booking.Services;

/// <summary>
/// Interface cho Room Service
/// </summary>
public interface IRoomService
{
    Task<RoomReadDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<RoomListDto>> ListByCinemaAsync(Guid cinemaId, RoomSearchDto searchDto, CancellationToken ct = default);
    Task<RoomReadDto?> CreateAsync(Guid cinemaId, CreateRoomDto dto, CancellationToken ct = default);
    Task<RoomReadDto?> UpdateAsync(Guid id, UpdateRoomDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<RoomReadDto?> ChangeStatusAsync(Guid id, ChangeRoomStatusDto dto, CancellationToken ct = default);
    Task<RoomStatsDto> GetStatsAsync(Guid id, CancellationToken ct = default);
}

/// <summary>
/// Service để xử lý business logic cho Room
/// </summary>
public class RoomService : IRoomService
{
    private readonly IRoomRepository _roomRepository;
    private readonly ICinemaRepository _cinemaRepository;

    public RoomService(IRoomRepository roomRepository, ICinemaRepository cinemaRepository)
    {
        _roomRepository = roomRepository;
        _cinemaRepository = cinemaRepository;
    }

    public async Task<RoomReadDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var room = await _roomRepository.GetByIdWithCinemaAsync(id, ct);
        return room == null ? null : MapToReadDto(room);
    }

    public async Task<PagedResult<RoomListDto>> ListByCinemaAsync(Guid cinemaId, RoomSearchDto searchDto, CancellationToken ct = default)
    {
        var (rooms, total) = await _roomRepository.ListByCinemaAsync(cinemaId, searchDto, ct);
        var items = rooms.Select(MapToListDto).ToList();

        return new PagedResult<RoomListDto>
        {
            Items = items,
            Page = searchDto.Page,
            PageSize = searchDto.PageSize,
            TotalItems = total
        };
    }

    public async Task<RoomReadDto?> CreateAsync(Guid cinemaId, CreateRoomDto dto, CancellationToken ct = default)
    {
        // Check if cinema exists and is active
        var cinema = await _cinemaRepository.GetByIdAsync(cinemaId, ct);
        if (cinema == null)
        {
            throw new ArgumentException("Rạp chiếu phim không tồn tại");
        }

        if (cinema.Status != EntityStatus.Active)
        {
            throw new InvalidOperationException("Không thể tạo phòng chiếu cho rạp chiếu phim không hoạt động");
        }

        // Check if room code already exists in the same cinema
        var codeExists = await _roomRepository.ExistsByCodeAsync(dto.Code, cinemaId, ct);
        if (codeExists)
        {
            throw new ArgumentException($"Mã phòng chiếu '{dto.Code}' đã tồn tại trong rạp này");
        }

        var room = new Room
        {
            Id = Guid.NewGuid(),
            CinemaId = cinemaId,
            Name = dto.Name,
            Code = dto.Code,
            TotalSeats = dto.TotalSeats,
            Status = EntityStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        var createdRoom = await _roomRepository.AddAsync(room, ct);
        return createdRoom == null ? null : MapToReadDto(createdRoom);
    }

    public async Task<RoomReadDto?> UpdateAsync(Guid id, UpdateRoomDto dto, CancellationToken ct = default)
    {
        var room = await _roomRepository.GetByIdWithCinemaAsync(id, ct);
        if (room == null) return null;

        // Check if room code already exists in the same cinema (excluding current room)
        var codeExists = await _roomRepository.ExistsByCodeAsync(dto.Code, room.CinemaId, id, ct);
        if (codeExists)
        {
            throw new ArgumentException($"Mã phòng chiếu '{dto.Code}' đã tồn tại trong rạp này");
        }

        // Update room properties
        room.Name = dto.Name;
        room.Code = dto.Code;
        room.TotalSeats = dto.TotalSeats;

        var updatedRoom = await _roomRepository.UpdateAsync(room, ct);
        return updatedRoom == null ? null : MapToReadDto(updatedRoom);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        // Check if room has active showtimes
        var hasActiveShowtimes = await _roomRepository.HasActiveShowtimesAsync(id, ct);
        if (hasActiveShowtimes)
        {
            throw new InvalidOperationException("Không thể xóa phòng chiếu có lịch chiếu đang hoạt động");
        }

        return await _roomRepository.DeleteAsync(id, ct);
    }

    public async Task<RoomReadDto?> ChangeStatusAsync(Guid id, ChangeRoomStatusDto dto, CancellationToken ct = default)
    {
        var room = await _roomRepository.GetByIdWithCinemaAsync(id, ct);
        if (room == null) return null;

        if (Enum.TryParse<EntityStatus>(dto.Status, out var newStatus))
        {
            // If changing to Inactive, check for active showtimes
            if (newStatus == EntityStatus.Inactive)
            {
                var hasActiveShowtimes = await _roomRepository.HasActiveShowtimesAsync(id, ct);
                if (hasActiveShowtimes)
                {
                    throw new InvalidOperationException("Không thể vô hiệu hóa phòng chiếu có lịch chiếu đang hoạt động");
                }
            }

            room.Status = newStatus;
            var updatedRoom = await _roomRepository.UpdateAsync(room, ct);
            return updatedRoom == null ? null : MapToReadDto(updatedRoom);
        }

        throw new ArgumentException("Trạng thái không hợp lệ");
    }

    public async Task<RoomStatsDto> GetStatsAsync(Guid id, CancellationToken ct = default)
    {
        return await _roomRepository.GetStatsAsync(id, ct);
    }

    /// <summary>
    /// Map Room entity to RoomReadDto
    /// </summary>
    private static RoomReadDto MapToReadDto(Room room)
    {
        return new RoomReadDto
        {
            Id = room.Id,
            CinemaId = room.CinemaId,
            CinemaName = room.Cinema.Name,
            Name = room.Name,
            Code = room.Code,
            TotalSeats = room.TotalSeats,
            Status = room.Status.ToString(),
            CreatedAt = room.CreatedAt
        };
    }

    /// <summary>
    /// Map Room entity to RoomListDto
    /// </summary>
    private static RoomListDto MapToListDto(Room room)
    {
        return new RoomListDto
        {
            Id = room.Id,
            CinemaId = room.CinemaId,
            CinemaName = room.Cinema.Name,
            Name = room.Name,
            Code = room.Code,
            TotalSeats = room.TotalSeats,
            Status = room.Status.ToString(),
            CreatedAt = room.CreatedAt
        };
    }
}

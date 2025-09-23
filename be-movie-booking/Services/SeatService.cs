using be_movie_booking.DTOs;
using be_movie_booking.Models;
using be_movie_booking.Repositories;

namespace be_movie_booking.Services;

/// <summary>
/// Interface cho Seat Service
/// </summary>
public interface ISeatService
{
    Task<SeatReadDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<SeatListDto>> ListByRoomAsync(Guid roomId, SeatSearchDto searchDto, CancellationToken ct = default);
    Task<SeatLayoutDto> GetSeatLayoutAsync(Guid roomId, CancellationToken ct = default);
    Task<SeatReadDto?> CreateAsync(Guid roomId, CreateSeatDto dto, CancellationToken ct = default);
    Task<List<SeatReadDto>> CreateBulkLayoutAsync(Guid roomId, CreateSeatLayoutDto dto, CancellationToken ct = default);
    Task<SeatReadDto?> UpdateAsync(Guid id, UpdateSeatDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<bool> DeleteByRoomAsync(Guid roomId, CancellationToken ct = default);
    Task<SeatStatsDto> GetStatsAsync(Guid roomId, CancellationToken ct = default);
    Task<List<SeatReadDto>> GetAvailableSeatsAsync(Guid roomId, CancellationToken ct = default);
    Task<List<SeatReadDto>> GetSeatsByTypeAsync(Guid roomId, string seatType, CancellationToken ct = default);
}

/// <summary>
/// Service để xử lý business logic cho Seat
/// </summary>
public class SeatService : ISeatService
{
    private readonly ISeatRepository _seatRepository;
    private readonly IRoomRepository _roomRepository;

    public SeatService(ISeatRepository seatRepository, IRoomRepository roomRepository)
    {
        _seatRepository = seatRepository;
        _roomRepository = roomRepository;
    }

    public async Task<SeatReadDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var seat = await _seatRepository.GetByIdWithRoomAsync(id, ct);
        return seat == null ? null : MapToReadDto(seat);
    }

    public async Task<PagedResult<SeatListDto>> ListByRoomAsync(Guid roomId, SeatSearchDto searchDto, CancellationToken ct = default)
    {
        var (seats, total) = await _seatRepository.ListByRoomWithPagingAsync(roomId, searchDto, ct);
        var items = seats.Select(MapToListDto).ToList();

        return new PagedResult<SeatListDto>
        {
            Items = items,
            Page = searchDto.Page,
            PageSize = searchDto.PageSize,
            TotalItems = total
        };
    }

    public async Task<SeatLayoutDto> GetSeatLayoutAsync(Guid roomId, CancellationToken ct = default)
    {
        return await _seatRepository.GetSeatLayoutAsync(roomId, ct);
    }

    public async Task<SeatReadDto?> CreateAsync(Guid roomId, CreateSeatDto dto, CancellationToken ct = default)
    {
        // Validate room exists
        var room = await _roomRepository.GetByIdAsync(roomId, ct);
        if (room == null)
        {
            throw new ArgumentException("Room not found", nameof(roomId));
        }

        // Validate seat doesn't exist
        if (await _seatRepository.ExistsAsync(roomId, dto.RowLabel, dto.SeatNumber, ct))
        {
            throw new InvalidOperationException($"Seat {dto.RowLabel}{dto.SeatNumber} already exists in this room");
        }

        // Parse enums
        if (!Enum.TryParse<SeatType>(dto.SeatType, out var seatType))
        {
            throw new ArgumentException("Invalid seat type", nameof(dto.SeatType));
        }

        var seat = new Seat
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            RowLabel = dto.RowLabel,
            SeatNumber = dto.SeatNumber,
            SeatType = seatType,
            IsActive = dto.IsActive,
            PositionX = dto.PositionX,
            PositionY = dto.PositionY,
            SpecialNotes = dto.SpecialNotes,
            CreatedAt = DateTime.UtcNow
        };

        var createdSeat = await _seatRepository.CreateAsync(seat, ct);
        return MapToReadDto(createdSeat);
    }

    public async Task<List<SeatReadDto>> CreateBulkLayoutAsync(Guid roomId, CreateSeatLayoutDto dto, CancellationToken ct = default)
    {
        // Validate room exists
        var room = await _roomRepository.GetByIdAsync(roomId, ct);
        if (room == null)
        {
            throw new ArgumentException("Room not found", nameof(roomId));
        }

        // Parse seat type
        if (!Enum.TryParse<SeatType>(dto.DefaultSeatType, out var seatType))
        {
            throw new ArgumentException("Invalid seat type", nameof(dto.DefaultSeatType));
        }

        var seats = new List<Seat>();
        var currentRowLabel = dto.RowStartLabel;
        var positionX = dto.StartPositionX ?? 0;
        var positionY = dto.StartPositionY ?? 0;

        for (int row = 0; row < dto.Rows; row++)
        {
            for (int seatNum = 1; seatNum <= dto.SeatsPerRow; seatNum++)
            {
                // Skip middle aisle if configured
                if (dto.SkipMiddleAisle && dto.MiddleAislePosition.HasValue && seatNum == dto.MiddleAislePosition.Value)
                {
                    continue;
                }

                var seat = new Seat
                {
                    Id = Guid.NewGuid(),
                    RoomId = roomId,
                    RowLabel = currentRowLabel,
                    SeatNumber = seatNum,
                    SeatType = seatType,
                    IsActive = true,
                    PositionX = positionX + (seatNum - 1) * (dto.SeatSpacingX ?? 50),
                    PositionY = positionY + row * (dto.SeatSpacingY ?? 50),
                    CreatedAt = DateTime.UtcNow
                };

                seats.Add(seat);
            }

            // Move to next row label
            currentRowLabel = GetNextRowLabel(currentRowLabel);
        }

        var createdSeats = await _seatRepository.CreateBulkAsync(seats, ct);
        return createdSeats.Select(MapToReadDto).ToList();
    }

    public async Task<SeatReadDto?> UpdateAsync(Guid id, UpdateSeatDto dto, CancellationToken ct = default)
    {
        var seat = await _seatRepository.GetByIdAsync(id, ct);
        if (seat == null) return null;

        // Validate seat doesn't exist (excluding current seat)
        if (await _seatRepository.ExistsAsync(seat.RoomId, dto.RowLabel, dto.SeatNumber, id, ct))
        {
            throw new InvalidOperationException($"Seat {dto.RowLabel}{dto.SeatNumber} already exists in this room");
        }

        // Update editable fields
        seat.RowLabel = dto.RowLabel;
        seat.SeatNumber = dto.SeatNumber;
        if (Enum.TryParse<SeatType>(dto.SeatType, out var parsedType))
        {
            seat.SeatType = parsedType;
        }
        seat.IsActive = dto.IsActive;
        seat.PositionX = dto.PositionX;
        seat.PositionY = dto.PositionY;
        seat.SpecialNotes = dto.SpecialNotes;
        seat.UpdatedAt = DateTime.UtcNow;

        var updatedSeat = await _seatRepository.UpdateAsync(seat, ct);
        return MapToReadDto(updatedSeat);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        return await _seatRepository.DeleteAsync(id, ct);
    }

    public async Task<bool> DeleteByRoomAsync(Guid roomId, CancellationToken ct = default)
    {
        return await _seatRepository.DeleteByRoomAsync(roomId, ct);
    }

    public async Task<SeatStatsDto> GetStatsAsync(Guid roomId, CancellationToken ct = default)
    {
        return await _seatRepository.GetStatsAsync(roomId, ct);
    }

    public async Task<List<SeatReadDto>> GetAvailableSeatsAsync(Guid roomId, CancellationToken ct = default)
    {
        var seats = await _seatRepository.GetAvailableSeatsAsync(roomId, ct);
        return seats.Select(MapToReadDto).ToList();
    }

    public async Task<List<SeatReadDto>> GetSeatsByTypeAsync(Guid roomId, string seatType, CancellationToken ct = default)
    {
        if (!Enum.TryParse<SeatType>(seatType, out var type))
        {
            throw new ArgumentException("Invalid seat type", nameof(seatType));
        }

        var seats = await _seatRepository.GetSeatsByTypeAsync(roomId, type, ct);
        return seats.Select(MapToReadDto).ToList();
    }

    #region Private Methods

    private static SeatReadDto MapToReadDto(Seat seat)
    {
        return new SeatReadDto
        {
            Id = seat.Id,
            RoomId = seat.RoomId,
            RowLabel = seat.RowLabel,
            SeatNumber = seat.SeatNumber,
            SeatType = seat.SeatType.ToString(),
            IsActive = seat.IsActive,
            PositionX = seat.PositionX,
            PositionY = seat.PositionY,
            SpecialNotes = seat.SpecialNotes,
            CreatedAt = seat.CreatedAt,
            UpdatedAt = seat.UpdatedAt
        };
    }

    private static SeatListDto MapToListDto(Seat seat)
    {
        return new SeatListDto
        {
            Id = seat.Id,
            RowLabel = seat.RowLabel,
            SeatNumber = seat.SeatNumber,
            SeatType = seat.SeatType.ToString(),
            IsActive = seat.IsActive,
        };
    }

    private static string GetNextRowLabel(string currentLabel)
    {
        if (string.IsNullOrEmpty(currentLabel)) return "A";

        var chars = currentLabel.ToCharArray();
        var lastChar = chars[^1];

        if (lastChar == 'Z')
        {
            // Handle overflow: A -> B -> ... -> Z -> AA -> AB -> ...
            if (chars.Length == 1)
            {
                return "AA";
            }
            else
            {
                // For simplicity, just increment the last character
                // In a real implementation, you might want to handle AA, AB, etc.
                return currentLabel[..^1] + "A";
            }
        }

        return currentLabel[..^1] + (char)(lastChar + 1);
    }

    #endregion
}

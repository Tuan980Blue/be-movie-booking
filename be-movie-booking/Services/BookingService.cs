using be_movie_booking.DTOs;
using be_movie_booking.Models;
using be_movie_booking.Repositories;
using System.Text.Json;

namespace be_movie_booking.Services;

/// <summary>
/// Interface cho Booking Service
/// </summary>
public interface IBookingService
{
    Task<BookingResponseDto?> CreateAsync(CreateBookingDto dto, Guid? userId, CancellationToken ct = default);
    Task<BookingResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<BookingResponseDto?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<PagedResult<BookingResponseDto>> ListAsync(BookingSearchDto searchDto, CancellationToken ct = default);
    Task<BookingResponseDto?> ConfirmAsync(Guid bookingId, Guid paymentId, CancellationToken ct = default);
    Task<BookingResponseDto?> CancelAsync(Guid bookingId, string? reason, CancellationToken ct = default);
}

/// <summary>
/// Service để xử lý business logic cho Booking
/// </summary>
public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IShowtimeRepository _showtimeRepository;
    private readonly ISeatRepository _seatRepository;
    private readonly ISeatLockService _seatLockService;
    private readonly IPricingService _pricingService;

    public BookingService(
        IBookingRepository bookingRepository,
        IShowtimeRepository showtimeRepository,
        ISeatRepository seatRepository,
        ISeatLockService seatLockService,
        IPricingService pricingService)
    {
        _bookingRepository = bookingRepository;
        _showtimeRepository = showtimeRepository;
        _seatRepository = seatRepository;
        _seatLockService = seatLockService;
        _pricingService = pricingService;
    }

    public async Task<BookingResponseDto?> CreateAsync(CreateBookingDto dto, Guid? userId, CancellationToken ct = default)
    {
        // 1. Validate showtime exists
        var showtime = await _showtimeRepository.GetByIdWithDetailsAsync(dto.ShowtimeId, ct);
        if (showtime == null)
        {
            throw new ArgumentException("Showtime không tồn tại");
        }

        // 2. Validate showtime hasn't started
        if (showtime.StartUtc <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("Suất chiếu đã bắt đầu hoặc đã kết thúc");
        }

        // 3. Validate seats exist and belong to same room
        var seats = await _seatRepository.GetByIdsAsync(dto.SeatIds, ct);
        if (seats.Count != dto.SeatIds.Count)
        {
            throw new ArgumentException("Một hoặc nhiều ghế không tồn tại");
        }

        if (seats.Any(s => s.RoomId != showtime.RoomId))
        {
            throw new ArgumentException("Tất cả ghế phải thuộc cùng một phòng chiếu");
        }

        if (seats.Any(s => !s.IsActive))
        {
            throw new ArgumentException("Một hoặc nhiều ghế không còn hoạt động");
        }

        // 4. Kiểm tra ghế đã được đặt chưa
        var areBooked = await _bookingRepository.AreSeatsBookedAsync(dto.ShowtimeId, dto.SeatIds, ct);
        if (areBooked)
        {
            throw new InvalidOperationException("Một hoặc nhiều ghế đã được đặt");
        }

        // 5. Calculate total price for each seat using PricingService
        var (bookingItems, totalAmount) = await CalculateBookingItemsAndTotalAsync(
            dto.ShowtimeId, 
            seats, 
            ct);
        
        if (bookingItems.Count != seats.Count)
        {
            throw new InvalidOperationException("Không thể tính giá cho tất cả ghế");
        }
        
        if (totalAmount <= 0)
        {
            throw new InvalidOperationException("Tổng tiền phải lớn hơn 0");
        }

        // 6. Generate unique booking code
        var bookingCode = await _bookingRepository.GenerateUniqueBookingCodeAsync(ct);

        // 7. Serialize customer info
        var customerContactJson = JsonSerializer.Serialize(dto.CustomerInfo);

        // 8. Create booking
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            Code = bookingCode,
            UserId = userId,
            Status = BookingStatus.Pending,
            TotalAmountMinor = totalAmount,
            Currency = "VND",
            CustomerContactJson = customerContactJson,
            Items = bookingItems,
            CreatedAt = DateTime.UtcNow
        };

        var createdBooking = await _bookingRepository.AddAsync(booking, ct);
        return MapToDto(createdBooking);
    }

    public async Task<BookingResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var booking = await _bookingRepository.GetByIdWithDetailsAsync(id, ct);
        return booking == null ? null : MapToDto(booking);
    }

    public async Task<BookingResponseDto?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var booking = await _bookingRepository.GetByCodeAsync(code, ct);
        return booking == null ? null : MapToDto(booking);
    }

    public async Task<PagedResult<BookingResponseDto>> ListAsync(BookingSearchDto searchDto, CancellationToken ct = default)
    {
        var (bookings, total) = await _bookingRepository.ListAsync(searchDto, ct);
        return new PagedResult<BookingResponseDto>
        {
            Items = bookings.Select(MapToDto).ToList(),
            Page = searchDto.Page,
            PageSize = searchDto.PageSize,
            TotalItems = total
        };
    }

    public async Task<BookingResponseDto?> ConfirmAsync(Guid bookingId, Guid paymentId, CancellationToken ct = default)
    {
        var booking = await _bookingRepository.GetByIdWithDetailsAsync(bookingId, ct);
        if (booking == null)
        {
            return null;
        }

        if (booking.Status != BookingStatus.Pending)
        {
            throw new InvalidOperationException($"Không thể xác nhận booking với trạng thái: {booking.Status}");
        }

        // Update booking status
        booking.Status = BookingStatus.Confirmed;
        booking.UpdatedAt = DateTime.UtcNow;

        // Update booking items status
        foreach (var item in booking.Items)
        {
            item.Status = BookingItemStatus.Confirmed;
        }

        // Generate tickets
        var tickets = new List<Ticket>();
        foreach (var item in booking.Items)
        {
            var ticketCode = await _bookingRepository.GenerateUniqueTicketCodeAsync(ct);
            var ticket = new Ticket
            {
                Id = Guid.NewGuid(),
                BookingId = booking.Id,
                ShowtimeId = item.ShowtimeId,
                SeatId = item.SeatId,
                TicketCode = ticketCode,
                Status = TicketStatus.Issued,
                IssuedAt = DateTime.UtcNow
                // TODO: Generate QR code if needed
            };
            tickets.Add(ticket);
        }

        booking.Tickets = tickets;

        // Unlock seats
        var showtimeId = booking.Items.First().ShowtimeId;
        var seatIds = booking.Items.Select(i => i.SeatId).ToList();
        await _seatLockService.UnlockSeatsAsync(new SeatUnlockRequestDto
        {
            ShowtimeId = showtimeId,
            SeatIds = seatIds,
            UserId = booking.UserId
        });

        var updatedBooking = await _bookingRepository.UpdateAsync(booking, ct);
        return MapToDto(updatedBooking);
    }

    public async Task<BookingResponseDto?> CancelAsync(Guid bookingId, string? reason, CancellationToken ct = default)
    {
        var booking = await _bookingRepository.GetByIdWithDetailsAsync(bookingId, ct);
        if (booking == null)
        {
            return null;
        }

        if (booking.Status != BookingStatus.Pending)
        {
            throw new InvalidOperationException($"Chỉ có thể hủy booking với trạng thái Pending. Trạng thái hiện tại: {booking.Status}");
        }

        // Update booking status
        booking.Status = BookingStatus.Canceled;
        booking.UpdatedAt = DateTime.UtcNow;

        // Update booking items status
        foreach (var item in booking.Items)
        {
            item.Status = BookingItemStatus.Canceled;
        }

        // Unlock seats
        var showtimeId = booking.Items.First().ShowtimeId;
        var seatIds = booking.Items.Select(i => i.SeatId).ToList();
        await _seatLockService.UnlockSeatsAsync(new SeatUnlockRequestDto
        {
            ShowtimeId = showtimeId,
            SeatIds = seatIds,
            UserId = booking.UserId
        });

        var updatedBooking = await _bookingRepository.UpdateAsync(booking, ct);
        return MapToDto(updatedBooking);
    }

    private static BookingResponseDto MapToDto(Booking booking)
    {
        CustomerInfoDto? customerInfo = null;
        if (!string.IsNullOrEmpty(booking.CustomerContactJson))
        {
            try
            {
                customerInfo = JsonSerializer.Deserialize<CustomerInfoDto>(booking.CustomerContactJson);
            }
            catch
            {
                // Ignore deserialization errors
            }
        }

        return new BookingResponseDto
        {
            Id = booking.Id,
            Code = booking.Code,
            UserId = booking.UserId,
            User = booking.User != null ? new UserInfoDto
            {
                Id = booking.User.Id,
                Email = booking.User.Email,
                FullName = booking.User.FullName
            } : null,
            Status = booking.Status,
            TotalAmountMinor = booking.TotalAmountMinor,
            Currency = booking.Currency,
            CustomerInfo = customerInfo,
            CreatedAt = booking.CreatedAt,
            UpdatedAt = booking.UpdatedAt,
            Items = booking.Items.Select(item => new BookingItemResponseDto
            {
                Id = item.Id,
                ShowtimeId = item.ShowtimeId,
                Showtime = new ShowtimeInfoDto
                {
                    Id = item.Showtime.Id,
                    MovieId = item.Showtime.MovieId,
                    MovieTitle = item.Showtime.Movie?.Title ?? "",
                    RoomId = item.Showtime.RoomId,
                    RoomName = item.Showtime.Room?.Name ?? "",
                    CinemaId = item.Showtime.Room?.CinemaId ?? Guid.Empty,
                    CinemaName = item.Showtime.Room?.Cinema?.Name ?? "",
                    StartUtc = item.Showtime.StartUtc,
                    EndUtc = item.Showtime.EndUtc,
                    Language = item.Showtime.Language,
                    Format = item.Showtime.Format.ToString()
                },
                SeatId = item.SeatId,
                Seat = new SeatInfoDto
                {
                    Id = item.Seat.Id,
                    RowLabel = item.Seat.RowLabel,
                    SeatNumber = item.Seat.SeatNumber,
                    SeatType = item.Seat.SeatType
                },
                SeatPriceMinor = item.SeatPriceMinor,
                PriceCategory = item.PriceCategory,
                Status = item.Status,
                CreatedAt = item.CreatedAt
            }).ToList(),
            Tickets = booking.Tickets.Select(ticket => new TicketResponseDto
            {
                Id = ticket.Id,
                TicketCode = ticket.TicketCode,
                TicketQr = ticket.TicketQr,
                ShowtimeId = ticket.ShowtimeId,
                SeatId = ticket.SeatId,
                Status = ticket.Status,
                IssuedAt = ticket.IssuedAt
            }).ToList()
        };
    }

    /// <summary>
    /// Tính toán giá cho từng ghế và tạo booking items
    /// Chỉ cần truyền SeatIds, không cần ShowtimeId
    /// </summary>
    private async Task<(List<BookingItem> items, int totalAmount)> CalculateBookingItemsAndTotalAsync(
        Guid showtimeId,
        List<Seat> seats,
        CancellationToken ct = default)
    {
        if (seats == null || !seats.Any())
        {
            throw new ArgumentException("Danh sách ghế không được trống");
        }

        var seatIds = seats.Select(s => s.Id).ToList();
        
        // Tính giá chỉ cần SeatIds, không cần ShowtimeId
        var quoteResult = await _pricingService.QuoteAsync(new PricingQuoteRequestDto
        {
            SeatIds = seatIds
        }, ct);

        // Validate response
        if (quoteResult.Quotes == null || quoteResult.Quotes.Count != seatIds.Count)
        {
            throw new InvalidOperationException("Không thể tính đủ giá cho tất cả ghế");
        }

        var bookingItems = new List<BookingItem>();

        foreach (var quote in quoteResult.Quotes)
        {
            var item = new BookingItem
            {
                Id = Guid.NewGuid(),
                ShowtimeId = showtimeId,
                SeatId = quote.SeatId,
                SeatPriceMinor = quote.PriceMinor,
                PriceCategory = "SeatType",
                Status = BookingItemStatus.Pending
            };

            bookingItems.Add(item);
        }

        return (bookingItems, quoteResult.TotalAmountMinor);
    }
}

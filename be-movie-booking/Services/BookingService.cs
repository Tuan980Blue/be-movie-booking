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
    Task<BookingDraftResponseDto?> CreateAsync(CreateBookingDto dto, Guid? userId, CancellationToken ct = default);
    Task<BookingResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<BookingResponseDto?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<BookingListLightResultDto> ListAsync(BookingSearchDto searchDto, CancellationToken ct = default);
    Task<BookingResponseDto?> ConfirmAsync(Guid bookingId, Guid paymentId, CancellationToken ct = default);
    Task<BookingResponseDto?> CancelAsync(Guid bookingId, string? reason, CancellationToken ct = default);
}

/// <summary>
/// Service để xử lý business logic cho Booking
/// </summary>
public class BookingService : IBookingService
{
    private IUserService _userService;
    private readonly IBookingRepository _bookingRepository;
    private readonly IBookingDraftRepository _bookingDraftRepository;
    private readonly IShowtimeRepository _showtimeRepository;
    private readonly ISeatRepository _seatRepository;
    private readonly ISeatLockService _seatLockService;
    private readonly IPricingService _pricingService;

    public BookingService(
        IUserService userService,
        IBookingRepository bookingRepository,
        IBookingDraftRepository bookingDraftRepository,
        IShowtimeRepository showtimeRepository,
        ISeatRepository seatRepository,
        ISeatLockService seatLockService,
        IPricingService pricingService)
    {
        _userService = userService;
        _bookingRepository = bookingRepository;
        _bookingDraftRepository = bookingDraftRepository;
        _showtimeRepository = showtimeRepository;
        _seatRepository = seatRepository;
        _seatLockService = seatLockService;
        _pricingService = pricingService;
    }

    public async Task<BookingDraftResponseDto?> CreateAsync(CreateBookingDto dto, Guid? userId, CancellationToken ct = default)
    {
        //Thông tin liên hệ khách hàng truy vấn từ UserService nếu userId != null
        string? customerContactJson = null;
        if (userId.HasValue)
        {
            var user = await _userService.GetByIdAsync(userId.Value, ct);
            if (user != null)
            {
                Console.WriteLine("Data User" + user);
                var customerInfo = new CustomerInfoDto
                {
                    FullName = user.FullName,
                    Email = user.Email,
                    Phone = user.Phone
                };
                customerContactJson = JsonSerializer.Serialize(customerInfo);
            }
        }
        
        // 1. Validate showtime exists
        var showtime = await _showtimeRepository.GetByIdWithDetailsAsync(dto.ShowtimeId, ct);
        if (showtime == null)
        {
            throw new ArgumentException("Suất chiếu không tồn tại");
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

        // 4. Kiểm tra ghế đã được đặt chưa (trong database)
        var areBooked = await _bookingRepository.AreSeatsBookedAsync(dto.ShowtimeId, dto.SeatIds, ct);
        if (areBooked)
        {
            throw new InvalidOperationException("Một hoặc nhiều ghế đã được đặt");
        }
        
        // 5. Lock ghế (đảm bảo ghế không bị đặt bởi người khác trong quá trình tạo booking)
        var lockResult = await _seatLockService.LockSeatsAsync(new SeatLockRequestDto
        {
            ShowtimeId = dto.ShowtimeId,
            SeatIds = dto.SeatIds,
            UserId = userId ?? Guid.Empty
        });
        if (lockResult.LockedSeatIds.Count != dto.SeatIds.Count)
        {
            throw new InvalidOperationException("Không thể khóa tất cả ghế yêu cầu");
        }
        
        // 6. Calculate total price for each seat using PricingService
        var seatIds = dto.SeatIds;
        var quoteResult = await _pricingService.QuoteAsync(new PricingQuoteRequestDto
        {
            SeatIds = seatIds
        }, ct);

        if (quoteResult.Quotes == null || quoteResult.Quotes.Count != seatIds.Count)
        {
            throw new InvalidOperationException("Không thể tính đủ giá cho tất cả ghế");
        }

        // 7. Create booking draft (chỉ lưu thông tin tối thiểu vào Redis)
        var bookingId = Guid.NewGuid();
        var draft = new BookingDraftDto
        {
            Id = bookingId,
            UserId = userId,
            ShowtimeId = dto.ShowtimeId,
            SeatIds = seatIds,
            SeatPricesMinor = quoteResult.Quotes.Select(q => q.PriceMinor).ToList(),
            TotalAmountMinor = quoteResult.TotalAmountMinor,
            CustomerContactJson = customerContactJson
        };

        // Lưu draft vào Redis với TTL 3 phút (đồng bộ với thời gian khóa ghế)  
        var ttl = TimeSpan.FromMinutes(3);
        await _bookingDraftRepository.SaveAsync(draft, ttl, ct);
        
        // Convert draft thành BookingDraftResponseDto để trả về (đơn giản hóa)
        return await MapDraftToSimpleResponseDtoAsync(draft, ttl, ct);
    }

    public async Task<BookingResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        // Tìm trong draft (Redis) trước
        var draft = await _bookingDraftRepository.GetByIdAsync(id, ct);
        if (draft != null)
        {
            return await MapDraftToResponseDtoAsync(draft, ct);
        }

        // Nếu không tìm thấy trong draft, tìm trong database (confirmed)
        var booking = await _bookingRepository.GetByIdWithDetailsAsync(id, ct);
        return booking == null ? null : await MapToDtoAsync(booking, ct);
    }

    public async Task<BookingResponseDto?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        // Draft không có code, chỉ tìm trong database (confirmed)
        var booking = await _bookingRepository.GetByCodeAsync(code, ct);
        return booking == null ? null : await MapToDtoAsync(booking, ct);
    }

    public async Task<BookingListLightResultDto> ListAsync(BookingSearchDto searchDto, CancellationToken ct = default)
    {
        var (items, total) = await _bookingRepository.ListAsync(searchDto, ct);
        return new BookingListLightResultDto
        {
            Items = items,
            Page = searchDto.Page,
            PageSize = searchDto.PageSize,
            TotalItems = total
        };
    }

    public async Task<BookingResponseDto?> ConfirmAsync(Guid bookingId, Guid paymentId, CancellationToken ct = default)
    {
        // Lấy draft từ Redis
        var draft = await _bookingDraftRepository.GetByIdAsync(bookingId, ct);
        if (draft == null)
        {
            // Nếu không tìm thấy trong draft, kiểm tra trong database (có thể đã được confirm trước đó)
            var existingBooking = await _bookingRepository.GetByIdWithDetailsAsync(bookingId, ct);
            if (existingBooking != null)
            {
                if (existingBooking.Status == BookingStatus.Confirmed)
                {
                    Console.WriteLine("Booking đã được xác nhận trước đó");
                    return await MapToDtoAsync(existingBooking, ct);
                }
                throw new InvalidOperationException($"Booking không ở trạng thái Pending. Trạng thái hiện tại: {existingBooking.Status}");
            }
            return null;
        }

        // Generate unique booking code
        var bookingCode = await _bookingRepository.GenerateUniqueBookingCodeAsync(ct);
        var createdAt = DateTime.UtcNow;

        // Convert draft thành Booking entity để lưu vào database
        var booking = new Booking
        {
            Id = draft.Id,
            Code = bookingCode,
            BookingQr = bookingCode, // Sử dụng BookingCode làm QR code
            UserId = draft.UserId,
            Status = BookingStatus.Confirmed,
            TotalAmountMinor = draft.TotalAmountMinor,
            Currency = "VND",
            CustomerContactJson = draft.CustomerContactJson,
            CreatedAt = createdAt,
            UpdatedAt = DateTime.UtcNow,
            Items = draft.SeatIds.Zip(draft.SeatPricesMinor, (seatId, priceMinor) => new BookingItem
            {
                Id = Guid.NewGuid(),
                BookingId = draft.Id,
                ShowtimeId = draft.ShowtimeId,
                SeatId = seatId,
                SeatPriceMinor = priceMinor,
                PriceCategory = "SeatType", // Có thể tính lại nếu cần
                Status = BookingItemStatus.Confirmed,
                CreatedAt = createdAt
            }).ToList()
        };

        // Generate tickets (không cần QR code cho từng ticket nữa, chỉ cần BookingQr)
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
                TicketQr = null, // Không cần QR code cho từng ticket, chỉ dùng BookingQr
                Status = TicketStatus.Issued,
                IssuedAt = DateTime.UtcNow
            };
            tickets.Add(ticket);
        }

        booking.Tickets = tickets;

        // Unlock seats
        await _seatLockService.UnlockSeatsAsync(new SeatUnlockRequestDto
        {
            ShowtimeId = draft.ShowtimeId,
            SeatIds = draft.SeatIds,
            UserId = draft.UserId ?? Guid.Empty
        });

        // Lưu vào database (confirmed booking)
        var confirmedBooking = await _bookingRepository.AddAsync(booking, ct);
        
        // Xóa draft khỏi Redis
        await _bookingDraftRepository.DeleteAsync(bookingId, ct);

        return await MapToDtoAsync(confirmedBooking, ct);
    }

    public async Task<BookingResponseDto?> CancelAsync(Guid bookingId, string? reason, CancellationToken ct = default)
    {
        // Tìm trong draft (Redis) trước
        var draft = await _bookingDraftRepository.GetByIdAsync(bookingId, ct);
        bool isDraft = draft != null;

        if (isDraft)
        {
            // Unlock seats
            await _seatLockService.UnlockSeatsAsync(new SeatUnlockRequestDto
            {
                ShowtimeId = draft!.ShowtimeId,
                SeatIds = draft.SeatIds,
                UserId = draft.UserId
            });

            // Xóa draft khỏi Redis
            await _bookingDraftRepository.DeleteAsync(bookingId, ct);
            
            // Trả về null vì draft đã bị xóa
            return null;
        }

        // Nếu không tìm thấy trong draft, tìm trong database
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
        var seatIdsDb = booking.Items.Select(i => i.SeatId).ToList();
        await _seatLockService.UnlockSeatsAsync(new SeatUnlockRequestDto
        {
            ShowtimeId = showtimeId,
            SeatIds = seatIdsDb,
            UserId = booking.UserId
        });

        // Update status trong database
        var updatedBooking = await _bookingRepository.UpdateAsync(booking, ct);
        return await MapToDtoAsync(updatedBooking, ct);
    }

    private async Task<BookingResponseDto> MapToDtoAsync(Booking booking, CancellationToken ct = default)
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

        // Load thêm thông tin showtime và seat nếu booking từ Redis (không có navigation properties)
        var items = new List<BookingItemResponseDto>();
        foreach (var item in booking.Items)
        {
            ShowtimeInfoDto? showtimeInfo = null;
            SeatInfoDto? seatInfo = null;

            // Nếu không có navigation properties, load từ repository
            if (item.Showtime == null || item.Seat == null)
            {
                var showtime = await _showtimeRepository.GetByIdWithDetailsAsync(item.ShowtimeId, ct);
                var seat = await _seatRepository.GetByIdAsync(item.SeatId, ct);

                if (showtime != null)
                {
                    showtimeInfo = new ShowtimeInfoDto
                    {
                        Id = showtime.Id,
                        MovieId = showtime.MovieId,
                        MovieTitle = showtime.Movie?.Title ?? "",
                        RoomId = showtime.RoomId,
                        RoomName = showtime.Room?.Name ?? "",
                        CinemaId = showtime.Room?.CinemaId ?? Guid.Empty,
                        CinemaName = showtime.Room?.Cinema?.Name ?? "",
                        StartUtc = showtime.StartUtc,
                        EndUtc = showtime.EndUtc,
                        Language = showtime.Language,
                        Format = showtime.Format.ToString()
                    };
                }

                if (seat != null)
                {
                    seatInfo = new SeatInfoDto
                    {
                        Id = seat.Id,
                        RowLabel = seat.RowLabel,
                        SeatNumber = seat.SeatNumber,
                        SeatType = seat.SeatType
                    };
                }
            }
            else
            {
                // Có navigation properties, sử dụng trực tiếp
                showtimeInfo = new ShowtimeInfoDto
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
                };

                seatInfo = new SeatInfoDto
                {
                    Id = item.Seat.Id,
                    RowLabel = item.Seat.RowLabel,
                    SeatNumber = item.Seat.SeatNumber,
                    SeatType = item.Seat.SeatType
                };
            }

            items.Add(new BookingItemResponseDto
            {
                Id = item.Id,
                ShowtimeId = item.ShowtimeId,
                Showtime = showtimeInfo ?? new ShowtimeInfoDto
                {
                    Id = item.ShowtimeId,
                    MovieId = Guid.Empty,
                    MovieTitle = "",
                    RoomId = Guid.Empty,
                    RoomName = "",
                    CinemaId = Guid.Empty,
                    CinemaName = "",
                    StartUtc = DateTime.MinValue,
                    EndUtc = DateTime.MinValue,
                    Language = "",
                    Format = ""
                },
                SeatId = item.SeatId,
                Seat = seatInfo ?? new SeatInfoDto
                {
                    Id = item.SeatId,
                    RowLabel = "",
                    SeatNumber = 0,
                    SeatType = SeatType.Standard
                },
                SeatPriceMinor = item.SeatPriceMinor,
                PriceCategory = item.PriceCategory,
                Status = item.Status,
                CreatedAt = item.CreatedAt
            });
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
            BookingQr = booking.BookingQr,
            CustomerInfo = customerInfo,
            CreatedAt = booking.CreatedAt,
            UpdatedAt = booking.UpdatedAt,
            Items = items,
            Tickets = booking.Tickets?.Select(ticket => new TicketResponseDto
            {
                Id = ticket.Id,
                TicketCode = ticket.TicketCode,
                TicketQr = ticket.TicketQr,
                ShowtimeId = ticket.ShowtimeId,
                SeatId = ticket.SeatId,
                Status = ticket.Status,
                IssuedAt = ticket.IssuedAt,
                CheckedInAt = ticket.CheckedInAt,
                CheckedInBy = ticket.CheckedInBy
            }).ToList() ?? new List<TicketResponseDto>()
        };
    }


    /// <summary>
    /// Convert BookingDraftDto thành BookingDraftResponseDto đơn giản (chỉ thông tin cần thiết cho draft)
    /// </summary>
    private async Task<BookingDraftResponseDto> MapDraftToSimpleResponseDtoAsync(BookingDraftDto draft, TimeSpan ttl, CancellationToken ct = default)
    {
        // Lấy thông tin khách hàng từ CustomerContactJson (nếu có)
        CustomerInfoDto? customerInfo = null;
        if (!string.IsNullOrEmpty(draft.CustomerContactJson))
        {
            try
            {
                customerInfo = JsonSerializer.Deserialize<CustomerInfoDto>(draft.CustomerContactJson);
            }
            catch
            {
                // Ignore deserialization errors
            }
        }

        // Load showtime từ database (chỉ cần thông tin cơ bản)
        var showtime = await _showtimeRepository.GetByIdWithDetailsAsync(draft.ShowtimeId, ct);
        if (showtime == null)
        {
            throw new InvalidOperationException("Không tìm thấy thông tin suất chiếu");
        }

        // Load seats từ database (chỉ cần thông tin cơ bản)
        var seats = await _seatRepository.GetByIdsAsync(draft.SeatIds, ct);
        var seatDict = seats.ToDictionary(s => s.Id);

        // Map seats đơn giản
        var draftSeats = new List<BookingDraftItemDto>();
        for (int i = 0; i < draft.SeatIds.Count; i++)
        {
            var seatId = draft.SeatIds[i];
            var priceMinor = i < draft.SeatPricesMinor.Count ? draft.SeatPricesMinor[i] : 0;
            var seat = seatDict.GetValueOrDefault(seatId);
            
            draftSeats.Add(new BookingDraftItemDto
            {
                SeatId = seatId,
                RowLabel = seat?.RowLabel ?? "",
                SeatNumber = seat?.SeatNumber ?? 0,
                SeatPriceMinor = priceMinor
            });
        }

        var createdAt = DateTime.UtcNow;
        return new BookingDraftResponseDto
        {
            Id = draft.Id,
            UserId = draft.UserId,
            Status = BookingStatus.Pending,
            TotalAmountMinor = draft.TotalAmountMinor,
            Currency = "VND",
            CustomerInfo = customerInfo, // Chỉ trả về CustomerInfo, không trả về User riêng
            Showtime = new SimpleShowtimeInfoDto
            {
                Id = showtime.Id,
                MovieTitle = showtime.Movie?.Title ?? "",
                CinemaName = showtime.Room?.Cinema?.Name ?? "",
                RoomName = showtime.Room?.Name ?? "",
                StartUtc = showtime.StartUtc,
                Format = showtime.Format.ToString()
            },
            Seats = draftSeats,
            CreatedAt = createdAt,
            ExpiresAt = createdAt.Add(ttl) // Thời gian hết hạn draft
        };
    }

    /// <summary>
    /// Convert BookingDraftDto thành BookingResponseDto đầy đủ (dùng cho GetById khi là draft)
    /// </summary>
    private async Task<BookingResponseDto> MapDraftToResponseDtoAsync(BookingDraftDto draft, CancellationToken ct = default)
    {
        CustomerInfoDto? customerInfo = null;
        if (!string.IsNullOrEmpty(draft.CustomerContactJson))
        {
            try
            {
                customerInfo = JsonSerializer.Deserialize<CustomerInfoDto>(draft.CustomerContactJson);
            }
            catch
            {
                // Ignore deserialization errors
            }
        }

        // Load showtime và seats từ database
        var showtime = await _showtimeRepository.GetByIdWithDetailsAsync(draft.ShowtimeId, ct);
        if (showtime == null)
        {
            throw new InvalidOperationException("Không tìm thấy thông tin suất chiếu");
        }

        var seats = await _seatRepository.GetByIdsAsync(draft.SeatIds, ct);
        var seatDict = seats.ToDictionary(s => s.Id);

        // Map items từ SeatIds và SeatPricesMinor
        var items = new List<BookingItemResponseDto>();
        for (int i = 0; i < draft.SeatIds.Count; i++)
        {
            var seatId = draft.SeatIds[i];
            var priceMinor = i < draft.SeatPricesMinor.Count ? draft.SeatPricesMinor[i] : 0;
            var seat = seatDict.GetValueOrDefault(seatId);
            
            items.Add(new BookingItemResponseDto
            {
                Id = Guid.NewGuid(), // Generate temp ID cho response
                ShowtimeId = draft.ShowtimeId,
                Showtime = new ShowtimeInfoDto
                {
                    Id = showtime.Id,
                    MovieId = showtime.MovieId,
                    MovieTitle = showtime.Movie?.Title ?? "",
                    RoomId = showtime.RoomId,
                    RoomName = showtime.Room?.Name ?? "",
                    CinemaId = showtime.Room?.CinemaId ?? Guid.Empty,
                    CinemaName = showtime.Room?.Cinema?.Name ?? "",
                    StartUtc = showtime.StartUtc,
                    EndUtc = showtime.EndUtc,
                    Language = showtime.Language,
                    Format = showtime.Format.ToString()
                },
                SeatId = seatId,
                Seat = seat != null ? new SeatInfoDto
                {
                    Id = seat.Id,
                    RowLabel = seat.RowLabel,
                    SeatNumber = seat.SeatNumber,
                    SeatType = seat.SeatType
                } : new SeatInfoDto
                {
                    Id = seatId,
                    RowLabel = "",
                    SeatNumber = 0,
                    SeatType = SeatType.Standard
                },
                SeatPriceMinor = priceMinor,
                PriceCategory = "SeatType",
                Status = BookingItemStatus.Pending,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Load user info nếu có userId
        UserInfoDto? userInfo = null;
        if (draft.UserId.HasValue)
        {
            var user = await _userService.GetByIdAsync(draft.UserId.Value, ct);
            if (user != null)
            {
                userInfo = new UserInfoDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName
                };
            }
        }

        return new BookingResponseDto
        {
            Id = draft.Id,
            Code = "", // Draft không có code, sẽ generate khi confirm
            UserId = draft.UserId,
            User = userInfo,
            Status = BookingStatus.Pending,
            TotalAmountMinor = draft.TotalAmountMinor,
            Currency = "VND",
            CustomerInfo = customerInfo,
            CreatedAt = DateTime.UtcNow, // Có thể lấy từ Redis TTL nếu cần chính xác hơn
            UpdatedAt = null,
            Items = items,
            Tickets = new List<TicketResponseDto>() // Draft không có tickets
        };
    }

}

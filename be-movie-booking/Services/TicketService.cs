using be_movie_booking.DTOs;
using be_movie_booking.Models;
using be_movie_booking.Repositories;
using System.Text.Json;

namespace be_movie_booking.Services;

/// <summary>
/// Interface cho Ticket Service
/// </summary>
public interface ITicketService
{
    Task<BookingVerifyResponseDto?> VerifyBookingByQrCodeAsync(string qrCode, CancellationToken ct = default);
    Task<BookingVerifyResponseDto?> CheckInBookingAsync(string bookingCode, Guid staffId, CancellationToken ct = default);
}

/// <summary>
/// Service để xử lý business logic cho Ticket (verify QR, check-in)
/// </summary>
public class TicketService : ITicketService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly ITicketRepository _ticketRepository;

    public TicketService(IBookingRepository bookingRepository, ITicketRepository ticketRepository)
    {
        _bookingRepository = bookingRepository;
        _ticketRepository = ticketRepository;
    }

    /// <summary>
    /// Verify QR code (BookingCode) và trả về thông tin booking với tất cả tickets
    /// </summary>
    public async Task<BookingVerifyResponseDto?> VerifyBookingByQrCodeAsync(string qrCode, CancellationToken ct = default)
    {
        // QR code là BookingCode
        var bookingCode = qrCode.Trim().ToUpper();

        var booking = await _bookingRepository.GetByCodeAsync(bookingCode, ct);
        if (booking == null)
        {
            return new BookingVerifyResponseDto
            {
                BookingCode = bookingCode,
                IsValid = false,
                ValidationMessage = "Mã đơn hàng không tồn tại",
                IsFullyCheckedIn = false,
                Booking = new BookingInfoDto(),
                Tickets = new List<TicketInfoDto>()
            };
        }

        // Validate booking
        var isValid = true;
        var validationMessage = "";

        // Kiểm tra booking status
        if (booking.Status != BookingStatus.Confirmed)
        {
            isValid = false;
            validationMessage = $"Đơn hàng không hợp lệ. Trạng thái: {booking.Status}";
        }

        // Kiểm tra có tickets không
        if (!booking.Tickets.Any())
        {
            isValid = false;
            validationMessage = "Đơn hàng không có vé nào";
        }

        // Kiểm tra showtime đã qua chưa (lấy showtime đầu tiên)
        var now = DateTime.UtcNow;
        var firstTicket = booking.Tickets.FirstOrDefault();
        if (firstTicket != null && firstTicket.Showtime != null)
        {
            var showtimeStart = firstTicket.Showtime.StartUtc;
            var timeUntilShowtime = showtimeStart - now;

            // Cho phép check-in từ 60 phút trước giờ chiếu đến 30 phút sau giờ chiếu
            if (timeUntilShowtime.TotalMinutes < -30)
            {
                isValid = false;
                validationMessage = "Suất chiếu đã qua quá lâu (quá 30 phút)";
            }

            if (isValid && timeUntilShowtime.TotalMinutes > 60)
            {
                validationMessage = "Chưa đến thời gian check-in (chỉ được check-in từ 60 phút trước giờ chiếu)";
            }
        }

        // Map tất cả tickets
        var tickets = new List<TicketInfoDto>();
        var allCheckedIn = true;
        foreach (var ticket in booking.Tickets)
        {
            if (ticket.Status == TicketStatus.Void)
            {
                isValid = false;
                validationMessage = "Có vé đã bị hủy";
            }

            if (!ticket.CheckedInAt.HasValue)
            {
                allCheckedIn = false;
            }

            tickets.Add(new TicketInfoDto
            {
                Id = ticket.Id,
                TicketCode = ticket.TicketCode,
                ShowtimeId = ticket.ShowtimeId,
                Showtime = ticket.Showtime != null ? new ShowtimeInfoDto
                {
                    Id = ticket.Showtime.Id,
                    MovieId = ticket.Showtime.MovieId,
                    MovieTitle = ticket.Showtime.Movie?.Title ?? "",
                    RoomId = ticket.Showtime.RoomId,
                    RoomName = ticket.Showtime.Room?.Name ?? "",
                    CinemaId = ticket.Showtime.Room?.CinemaId ?? Guid.Empty,
                    CinemaName = ticket.Showtime.Room?.Cinema?.Name ?? "",
                    StartUtc = ticket.Showtime.StartUtc,
                    EndUtc = ticket.Showtime.EndUtc,
                    Language = ticket.Showtime.Language,
                    Format = ticket.Showtime.Format.ToString()
                } : new ShowtimeInfoDto
                {
                    Id = ticket.ShowtimeId,
                    MovieTitle = "",
                    RoomName = "",
                    CinemaName = "",
                    StartUtc = DateTime.MinValue,
                    EndUtc = DateTime.MinValue,
                    Format = ""
                },
                SeatId = ticket.SeatId,
                Seat = ticket.Seat != null ? new SeatInfoDto
                {
                    Id = ticket.Seat.Id,
                    RowLabel = ticket.Seat.RowLabel,
                    SeatNumber = ticket.Seat.SeatNumber,
                    SeatType = ticket.Seat.SeatType
                } : new SeatInfoDto
                {
                    Id = ticket.SeatId,
                    RowLabel = "",
                    SeatNumber = 0,
                    SeatType = SeatType.Standard
                },
                Status = ticket.Status,
                IssuedAt = ticket.IssuedAt,
                CheckedInAt = ticket.CheckedInAt
            });
        }

        // Map booking info
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

        var bookingInfo = new BookingInfoDto
        {
            Id = booking.Id,
            Code = booking.Code,
            Status = booking.Status,
            CustomerInfo = customerInfo,
            CreatedAt = booking.CreatedAt
        };

        return new BookingVerifyResponseDto
        {
            BookingCode = bookingCode,
            IsValid = isValid,
            ValidationMessage = isValid ? null : validationMessage,
            IsFullyCheckedIn = allCheckedIn,
            Booking = bookingInfo,
            Tickets = tickets
        };
    }

    /// <summary>
    /// Check-in booking (check-in tất cả tickets trong booking)
    /// </summary>
    public async Task<BookingVerifyResponseDto?> CheckInBookingAsync(string bookingCode, Guid staffId, CancellationToken ct = default)
    {
        var booking = await _bookingRepository.GetByCodeAsync(bookingCode.Trim().ToUpper(), ct);
        if (booking == null)
        {
            return null;
        }

        // Validate trước khi check-in
        var verifyBeforeCheckIn = await VerifyBookingByQrCodeAsync(bookingCode, ct);
        if (verifyBeforeCheckIn == null || !verifyBeforeCheckIn.IsValid)
        {
            return verifyBeforeCheckIn;
        }

        // Kiểm tra đã check-in hết chưa
        if (verifyBeforeCheckIn.IsFullyCheckedIn)
        {
            verifyBeforeCheckIn.ValidationMessage = "Tất cả vé trong đơn hàng đã được check-in trước đó";
            return verifyBeforeCheckIn;
        }

        // Check-in tất cả tickets chưa được check-in
        var checkInTime = DateTime.UtcNow;
        foreach (var ticket in booking.Tickets)
        {
            if (!ticket.CheckedInAt.HasValue)
            {
                ticket.CheckedInAt = checkInTime;
                ticket.CheckedInBy = staffId;
                await _ticketRepository.UpdateAsync(ticket, ct);
            }
        }

        // Trả về kết quả sau khi check-in
        return await VerifyBookingByQrCodeAsync(bookingCode, ct);
    }
}


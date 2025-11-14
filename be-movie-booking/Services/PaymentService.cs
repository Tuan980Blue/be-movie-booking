using be_movie_booking.DTOs;
using be_movie_booking.Models;
using be_movie_booking.Repositories;
using be_movie_booking.Services;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace be_movie_booking.Services;

public interface IPaymentService
{
    Task<PaymentResponseDto> CreatePaymentAsync(CreatePaymentDto dto, string clientIp, CancellationToken ct = default);
    Task<PaymentResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(List<PaymentResponseDto> Items, int Total)> SearchAsync(PaymentSearchDto searchDto, CancellationToken ct = default);
    Task<PaymentResponseDto?> ProcessVnPayReturnAsync(IQueryCollection query, CancellationToken ct = default);
    Task<PaymentResponseDto?> ProcessVnPayIpnAsync(IQueryCollection query, CancellationToken ct = default);
}

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IVnPayService _vnPayService;
    private readonly IBookingService _bookingService;
    private readonly IBookingDraftRepository _bookingDraftRepository;
    private readonly ISeatLockService _seatLockService;
    private readonly IConfiguration _configuration;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IVnPayService vnPayService,
        IBookingService bookingService,
        IBookingDraftRepository bookingDraftRepository,
        ISeatLockService seatLockService,
        IConfiguration configuration)
    {
        _paymentRepository = paymentRepository;
        _vnPayService = vnPayService;
        _bookingService = bookingService;
        _bookingDraftRepository = bookingDraftRepository;
        _seatLockService = seatLockService;
        _configuration = configuration;
    }

    public async Task<PaymentResponseDto> CreatePaymentAsync(CreatePaymentDto dto, string clientIp, CancellationToken ct = default)
    {
        // Get booking to validate and get amount
        var booking = await _bookingService.GetByIdAsync(dto.BookingId, ct);
        if (booking == null)
        {
            throw new InvalidOperationException("Booking không tồn tại");
        }

        if (booking.Status != BookingStatus.Pending)
        {
            throw new InvalidOperationException($"Booking không ở trạng thái Pending. Trạng thái hiện tại: {booking.Status}");
        }

        // Check if payment already exists for this booking
        var existingPayment = await _paymentRepository.GetByBookingIdAsync(dto.BookingId, ct);
        if (existingPayment != null && existingPayment.Status == PaymentStatus.Pending)
        {
            // Return existing payment URL if still pending
            return MapToDto(existingPayment);
        }

        // Create new payment
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            BookingId = dto.BookingId,
            Provider = dto.Provider,
            AmountMinor = booking.TotalAmountMinor,
            Currency = booking.Currency,
            Status = PaymentStatus.Initiated,
            ReturnUrl = dto.ReturnUrl,
            NotifyUrl = dto.NotifyUrl,
            CreatedAt = DateTime.UtcNow
        };

        payment = await _paymentRepository.CreateAsync(payment, ct);

        // Get booking draft to get showtimeId and seatIds for seat lock extension
        var draft = await _bookingDraftRepository.GetByIdAsync(dto.BookingId, ct);
        if (draft != null)
        {
            // Extend draft TTL to allow more time for payment processing (10 minutes)
            // This ensures the draft doesn't expire before payment completes
            await _bookingDraftRepository.ExtendTtlAsync(dto.BookingId, TimeSpan.FromMinutes(10), ct);

            // Extend seat locks to match draft TTL extension (10 minutes)
            // This prevents seats from being unlocked before payment completes
            if (draft.SeatIds != null && draft.SeatIds.Count > 0)
            {
                try
                {
                    var lockExtensionResult = await _seatLockService.ChangeTimeLockSeatsAsync(new SeatLockExtendRequestDto
                    {
                        ShowtimeId = draft.ShowtimeId,
                        SeatIds = draft.SeatIds,
                        UserId = draft.UserId
                    });

                    if (!lockExtensionResult.Success)
                    {
                        // Log warning but don't fail payment creation
                        Console.WriteLine($"Warning: Failed to extend seat locks for booking {dto.BookingId}: {lockExtensionResult.Message}");
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't fail payment creation
                    Console.WriteLine($"Error extending seat locks for booking {dto.BookingId}: {ex.Message}");
                }
            }
        }

        // Add payment event
        await _paymentRepository.AddEventAsync(new PaymentEvent
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            EventType = "created",
            RawPayloadJson = JsonSerializer.Serialize(new { dto, clientIp }),
            CreatedAt = DateTime.UtcNow
        }, ct);

        // Generate payment URL if VNPay
        string? paymentUrl = null;
        if (dto.Provider == PaymentProvider.VnPay)
        {
            // Ưu tiên dùng returnUrl từ frontend, nếu không có thì dùng config
            var returnUrl = dto.ReturnUrl 
                ?? _configuration["VNPay:vnp_ReturnUrl"] 
                ?? throw new InvalidOperationException("VNPay returnUrl is not configured");
            
            // Draft booking không có Code, sử dụng BookingId thay thế
            var bookingIdentifier = string.IsNullOrEmpty(booking.Code) 
                ? booking.Id.ToString() 
                : booking.Code;
            paymentUrl = _vnPayService.CreatePaymentUrl(
                orderId: payment.Id.ToString(),
                amount: booking.TotalAmountMinor, // VNPay expects amount in VND (minor units)
                orderDescription: $"Thanh toán đặt vé - Booking {bookingIdentifier}",
                ipAddress: clientIp,
                returnUrl: returnUrl
            );

            payment.Status = PaymentStatus.Pending;
            payment = await _paymentRepository.UpdateAsync(payment, ct);
        }

        var response = MapToDto(payment);
        response.PaymentUrl = paymentUrl;
        return response;
    }

    public async Task<PaymentResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var payment = await _paymentRepository.GetByIdAsync(id, ct);
        return payment != null ? MapToDto(payment) : null;
    }

    public async Task<(List<PaymentResponseDto> Items, int Total)> SearchAsync(PaymentSearchDto searchDto, CancellationToken ct = default)
    {
        var (items, total) = await _paymentRepository.SearchAsync(
            searchDto.BookingId,
            searchDto.Provider,
            searchDto.Status,
            searchDto.Page,
            searchDto.PageSize,
            searchDto.SortBy,
            searchDto.SortOrder,
            ct
        );

        var dtos = items.Select(MapToDto).ToList();
        return (dtos, total);
    }

    public async Task<PaymentResponseDto?> ProcessVnPayReturnAsync(IQueryCollection query, CancellationToken ct = default)
    {
        // Validate signature
        if (!_vnPayService.ValidatePaymentResponse(query))
        {
            throw new InvalidOperationException("Invalid VNPay response signature");
        }

        //Dữ liệu trả về từ VNPay
        var responseData = _vnPayService.GetResponseData(query);
        
        var vnp_TxnRef = responseData.GetValueOrDefault("vnp_TxnRef");
        var vnp_ResponseCode = responseData.GetValueOrDefault("vnp_ResponseCode");
        var vnp_TransactionNo = responseData.GetValueOrDefault("vnp_TransactionNo");
        
        //Lấy paymentId từ vnp_TxnRef
        if (string.IsNullOrEmpty(vnp_TxnRef) || !Guid.TryParse(vnp_TxnRef, out var paymentId))
        {
            throw new InvalidOperationException("Invalid payment ID in VNPay response");
        }
        
        // Không cần include Booking vì booking có thể chỉ tồn tại trong Redis (draft)
        // Chỉ cần payment.BookingId để confirm booking sau
        var payment = await _paymentRepository.GetByIdAsync(paymentId, ct);
        
        if (payment == null)
        {
            throw new InvalidOperationException("Payment not found");
        }

        // Add callback event
        await _paymentRepository.AddEventAsync(new PaymentEvent
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            EventType = "return_received",
            RawPayloadJson = JsonSerializer.Serialize(responseData),
            CreatedAt = DateTime.UtcNow
        }, ct);

        var isSuccess = vnp_ResponseCode == "00";
        var shouldUpdate =
            (isSuccess && payment.Status != PaymentStatus.Succeeded) ||
            (!isSuccess && payment.Status == PaymentStatus.Pending);

        if (shouldUpdate)
        {
            if (isSuccess)
            {
                payment.Status = PaymentStatus.Succeeded;
                payment.ProviderTxnId = vnp_TransactionNo;
            }
            else
            {
                payment.Status = PaymentStatus.Failed;
            }

            payment = await _paymentRepository.UpdateAsync(payment, ct);

            if (isSuccess)
            {
                try
                {
                    await _bookingService.ConfirmAsync(payment.BookingId, payment.Id, ct);
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine($"Booking confirmation result (return): {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error confirming booking after VNPay return: {ex.Message}");
                }
            }
        }

        return MapToDto(payment);
    }

    public async Task<PaymentResponseDto?> ProcessVnPayIpnAsync(IQueryCollection query, CancellationToken ct = default)
    {
        // Validate signature
        if (!_vnPayService.ValidatePaymentResponse(query))
        {
            throw new InvalidOperationException("Invalid VNPay IPN signature");
        }

        var responseData = _vnPayService.GetResponseData(query);
        var vnp_TxnRef = responseData.GetValueOrDefault("vnp_TxnRef");
        var vnp_ResponseCode = responseData.GetValueOrDefault("vnp_ResponseCode");
        var vnp_TransactionNo = responseData.GetValueOrDefault("vnp_TransactionNo");

        if (string.IsNullOrEmpty(vnp_TxnRef) || !Guid.TryParse(vnp_TxnRef, out var paymentId))
        {
            throw new InvalidOperationException("Invalid payment ID in VNPay IPN");
        }

        // Không cần include Booking vì booking có thể chỉ tồn tại trong Redis (draft)
        // Chỉ cần payment.BookingId để confirm booking sau
        var payment = await _paymentRepository.GetByIdAsync(paymentId, ct);
        if (payment == null)
        {
            throw new InvalidOperationException("Payment not found");
        }

        // Add IPN event
        await _paymentRepository.AddEventAsync(new PaymentEvent
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            EventType = "ipn_received",
            RawPayloadJson = JsonSerializer.Serialize(responseData),
            CreatedAt = DateTime.UtcNow
        }, ct);

        // Update payment status based on response code
        if (vnp_ResponseCode == "00" && payment.Status != PaymentStatus.Succeeded)
        {
            payment.Status = PaymentStatus.Succeeded;
            payment.ProviderTxnId = vnp_TransactionNo;
            payment = await _paymentRepository.UpdateAsync(payment, ct);

            // If payment succeeded, confirm the booking
            // Booking might be in Redis (draft) or database (if already confirmed)
            try
            {
                await _bookingService.ConfirmAsync(payment.BookingId, payment.Id, ct);
            }
            catch (InvalidOperationException ex)
            {
                // If booking is already confirmed or doesn't exist, that's okay
                // Log but don't fail the IPN processing
                Console.WriteLine($"Booking confirmation result: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Log error but don't fail the IPN processing
                Console.WriteLine($"Error confirming booking after payment: {ex.Message}");
            }
        }
        else if (vnp_ResponseCode != "00" && payment.Status == PaymentStatus.Pending)
        {
            payment.Status = PaymentStatus.Failed;
            payment = await _paymentRepository.UpdateAsync(payment, ct);
        }

        return MapToDto(payment);
    }

    private static PaymentResponseDto MapToDto(Payment payment)
    {
        return new PaymentResponseDto
        {
            Id = payment.Id,
            BookingId = payment.BookingId,
            Provider = payment.Provider,
            AmountMinor = payment.AmountMinor,
            Currency = payment.Currency,
            Status = payment.Status,
            ProviderTxnId = payment.ProviderTxnId,
            CreatedAt = payment.CreatedAt,
            UpdatedAt = payment.UpdatedAt
        };
    }
}

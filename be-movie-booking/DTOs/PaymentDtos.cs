using System.ComponentModel.DataAnnotations;
using be_movie_booking.Models;

namespace be_movie_booking.DTOs;

/// <summary>
/// DTO to create a new payment
/// </summary>
public class CreatePaymentDto
{
    [Required(ErrorMessage = "BookingId là bắt buộc")]
    public Guid BookingId { get; set; }
    
    [Required(ErrorMessage = "Provider là bắt buộc")]
    public PaymentProvider Provider { get; set; }
    
    public string? ReturnUrl { get; set; }
    public string? NotifyUrl { get; set; }
}

/// <summary>
/// DTO for payment response
/// </summary>
public class PaymentResponseDto
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public PaymentProvider Provider { get; set; }
    public int AmountMinor { get; set; }
    public string Currency { get; set; } = "VND";
    public PaymentStatus Status { get; set; }
    public string? ProviderTxnId { get; set; }
    public string? PaymentUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO for VNPay return callback
/// </summary>
public class VnPayReturnDto
{
    public string vnp_TxnRef { get; set; } = null!;
    public string vnp_Amount { get; set; } = null!;
    public string vnp_ResponseCode { get; set; } = null!;
    public string vnp_TransactionNo { get; set; } = null!;
    public string vnp_OrderInfo { get; set; } = null!;
    public string vnp_BankCode { get; set; } = null!;
    public string vnp_PayDate { get; set; } = null!;
    public string vnp_SecureHash { get; set; } = null!;
}

/// <summary>
/// DTO for payment search/filter
/// </summary>
public class PaymentSearchDto
{
    public Guid? BookingId { get; set; }
    public PaymentProvider? Provider { get; set; }
    public PaymentStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "CreatedAt";
    public string SortOrder { get; set; } = "desc";
}


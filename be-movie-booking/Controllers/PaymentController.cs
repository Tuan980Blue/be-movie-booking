using be_movie_booking.DTOs;
using be_movie_booking.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace be_movie_booking.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    /// <summary>
    /// Create a new payment
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<PaymentResponseDto>> CreatePayment([FromBody] CreatePaymentDto dto, CancellationToken ct)
    {
        try
        {
            var clientIp = GetClientIpAddress();
            var payment = await _paymentService.CreatePaymentAsync(dto, clientIp, ct);
            return Ok(payment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi tạo payment", error = ex.Message });
        }
    }

    /// <summary>
    /// Get payment by ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<PaymentResponseDto>> GetPayment(Guid id, CancellationToken ct)
    {
        var payment = await _paymentService.GetByIdAsync(id, ct);
        if (payment == null)
        {
            return NotFound(new { message = "Payment không tồn tại" });
        }
        return Ok(payment);
    }

    /// <summary>
    /// Search payments
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<ActionResult> SearchPayments([FromQuery] PaymentSearchDto searchDto, CancellationToken ct)
    {
        var (items, total) = await _paymentService.SearchAsync(searchDto, ct);
        return Ok(new
        {
            items,
            page = searchDto.Page,
            pageSize = searchDto.PageSize,
            total,
            totalPages = (int)Math.Ceiling(total / (double)searchDto.PageSize)
        });
    }

    /// <summary>
    /// VNPay return URL - called when user returns from VNPay payment page
    /// </summary>
    [HttpGet("vnpay-return")]
    public async Task<ActionResult> VnPayReturn(CancellationToken ct)
    {
        try
        {
            var payment = await _paymentService.ProcessVnPayReturnAsync(Request.Query, ct);
         
            if (payment == null)
            {
                return BadRequest(new { message = "Không tìm thấy payment" });
            }

            // Redirect to frontend with payment status
            var frontendUrl = payment.Status == Models.PaymentStatus.Succeeded
                ? $"http://localhost:3000/booking/payment/success?paymentId={payment.Id}"
                : $"http://localhost:3000/booking/payment/failed?paymentId={payment.Id}";

            return Redirect(frontendUrl);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi xử lý VNPay return", error = ex.Message });
        }
    }

    /// <summary>
    /// VNPay IPN (Instant Payment Notification) - called by VNPay server to notify payment status
    /// This is the secure way to process payments
    /// </summary>
    [HttpPost("vnpay-ipn")]
    public async Task<IActionResult> VnPayIpn(CancellationToken ct)
    {
        try
        {
            var payment = await _paymentService.ProcessVnPayIpnAsync(Request.Query, ct);
            if (payment == null)
            {
                return BadRequest(new { RspCode = "01", Message = "Payment not found" });
            }

            // Return success response to VNPay
            return Ok(new { RspCode = "00", Message = "Success" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { RspCode = "01", Message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { RspCode = "99", Message = "Internal error", Error = ex.Message });
        }
    }

    /// <summary>
    /// Get client IP address
    /// </summary>
    private string GetClientIpAddress()
    {
        // Check for forwarded IP (when behind proxy/load balancer)
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ips = forwardedFor.Split(',');
            if (ips.Length > 0)
            {
                return ips[0].Trim();
            }
        }

        // Check for real IP
        var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // Fallback to connection remote IP
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
    }
}

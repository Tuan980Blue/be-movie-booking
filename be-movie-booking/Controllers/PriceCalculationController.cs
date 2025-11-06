using be_movie_booking.DTOs;
using be_movie_booking.Services;
using Microsoft.AspNetCore.Mvc;

namespace be_movie_booking.Controllers;

/// <summary>
/// Controller để tính giá vé cho các ghế
/// </summary>
[ApiController]
[Route("api/price-calculation")]
public class PriceCalculationController : ControllerBase
{
    private readonly IPricingService _pricing;

    public PriceCalculationController(IPricingService pricing)
    {
        _pricing = pricing;
    }

    /// <summary>
    /// Tính giá vé cho các ghế - chỉ cần truyền danh sách SeatIds
    /// </summary>
    [HttpPost("calculate")]
    public async Task<IActionResult> Calculate([FromBody] PricingQuoteRequestDto request, CancellationToken ct)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _pricing.QuoteAsync(request, ct);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Có lỗi xảy ra khi tính giá", error = ex.Message });
        }
    }
}

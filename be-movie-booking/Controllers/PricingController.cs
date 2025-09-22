using be_movie_booking.DTOs;
using be_movie_booking.Services;
using Microsoft.AspNetCore.Mvc;

namespace be_movie_booking.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PricingController : ControllerBase
{
    private readonly IPricingService _pricing;

    public PricingController(IPricingService pricing)
    {
        _pricing = pricing;
    }

    [HttpPost("quote")]
    public async Task<IActionResult> Quote([FromBody] PricingQuoteRequestDto request, CancellationToken ct)
    {
        var quote = await _pricing.QuoteAsync(request, ct);
        return Ok(quote);
    }
}

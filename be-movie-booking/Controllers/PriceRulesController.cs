using be_movie_booking.DTOs;
using be_movie_booking.Hubs;
using be_movie_booking.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace be_movie_booking.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PriceRulesController : ControllerBase
{
    private readonly IPriceRuleService _service;
    private readonly IHubContext<AppHub> _hubContext;

    public PriceRulesController(IPriceRuleService service, IHubContext<AppHub> hubContext)
    {
        _service = service;
        _hubContext = hubContext;
    }

    /// <summary>
    /// Lấy danh sách quy định giá
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PriceRuleSearchDto search, CancellationToken ct)
    {
        var (items, total) = await _service.ListAsync(search, ct);
        return Ok(new { items, total });
    }

    /// <summary>
    /// Lấy chi tiết quy định giá theo ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var result = await _service.GetAsync(id, ct);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Tạo quy định giá mới (Admin/Manager only)
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PriceRuleCreateDto dto, CancellationToken ct)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var created = await _service.CreateAsync(dto, ct);
            if (created != null)
            {
                // Gửi thông báo real-time về việc giá đã được cập nhật
                await _hubContext.Clients.Group("price-rules").SendAsync("price_rules_updated", new
                {
                    Action = "created",
                    PriceRule = created
                });
            }
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Cập nhật quy định giá (Admin/Manager only)
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] PriceRuleUpdateDto dto, CancellationToken ct)
    {
        if (id != dto.Id) return BadRequest("Id mismatch");
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updated = await _service.UpdateAsync(id, dto, ct);
            if (updated == null) return NotFound();
            
            // Gửi thông báo real-time về việc giá đã được cập nhật
            await _hubContext.Clients.Group("price-rules").SendAsync("price_rules_updated", new
            {
                Action = "updated",
                PriceRule = updated
            });
            
            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Xóa quy định giá (Admin/Manager only)
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var ok = await _service.DeleteAsync(id, ct);
        if (!ok) return NotFound();
        
        // Gửi thông báo real-time về việc giá đã được xóa
        await _hubContext.Clients.Group("price-rules").SendAsync("price_rules_updated", new
        {
            Action = "deleted",
            Id = id
        });
        
        return NoContent();
    }
}

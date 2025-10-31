using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace be_movie_booking.Controllers;

[ApiController]
[Route("api/[controller]")]
// Controller mẫu để áp dụng giới hạn tần suất (rate limiting)
//mục đích là để bạn có thể triển khai các hành vi giới hạn tần suất cụ thể
//cho các endpoint kế thừa từ controller này.
public class RateLimitedController: ControllerBase
{
    private readonly IDatabase _db;

    public RateLimitedController(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    [HttpPost]
    [HttpGet]
    [Route("sliding-window")]
    // Ví dụ về giới hạn tần suất sử dụng kỹ thuật Sliding Window
    //cách sử dụng api này: /api/RateLimited/sliding-window?clientId=your_client_id
    public async Task<IActionResult> SlidingWindowRateLimit(string clientId)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var windowSize = 60; // 60 giây
        var maxRequests = 5; // tối đa 5 request trong cửa sổ thời gian 
        var windowStart = now - windowSize;
        var key = $"rate_limit:sliding_window:{clientId}";
        // Xóa các request cũ ngoài cửa sổ thời gian
        await _db.SortedSetRemoveRangeByScoreAsync(key, 0, windowStart);
        // Đếm số request trong cửa sổ thời gian hiện tại
        var requestCount = await _db.SortedSetLengthAsync(key);
        if (requestCount >= maxRequests)
        {
            return StatusCode(429, "Too Many Requests - Sliding Window Limit Exceeded");
        }

        // Thêm request hiện tại vào Redis
        await _db.SortedSetAddAsync(key, now.ToString(), now);
        // Đặt thời gian hết hạn cho key
        await _db.KeyExpireAsync(key, TimeSpan.FromSeconds(windowSize));
        return Ok("Request Successful - Within Sliding Window Limit");
    }
}
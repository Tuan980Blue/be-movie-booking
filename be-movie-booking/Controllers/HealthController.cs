using be_movie_booking.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace be_movie_booking.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {
        private readonly MovieBookingDbContext _dbContext;
        private readonly IConfiguration _configuration;

        public HealthController(MovieBookingDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        [HttpGet("db")]
        public async Task<IActionResult> CheckDatabaseAsync()
        {
            try
            {
                var canConnect = await _dbContext.Database.CanConnectAsync();
                if (canConnect)
                {
                    return Ok(new { status = "Healthy", service = "database" });
                }
                return StatusCode(503, new { status = "Unhealthy", service = "database" });
            }
            catch (Exception ex)
            {
                return StatusCode(503, new { status = "Unhealthy", service = "database", error = ex.Message });
            }
        }

        [HttpGet("redis")]
        public async Task<IActionResult> CheckRedisAsync()
        {
            var redisConnection = _configuration.GetValue<string>("Redis:Connection");
            if (string.IsNullOrWhiteSpace(redisConnection))
            {
                return StatusCode(503, new { status = "Unhealthy", service = "redis", error = "Missing Redis:Connection" });
            }

            try
            {
                using var mux = await ConnectionMultiplexer.ConnectAsync(redisConnection);
                var db = mux.GetDatabase();
                _ = await db.PingAsync();
                return Ok(new { status = "Healthy", service = "redis" });
            }
            catch (Exception ex)
            {
                return StatusCode(503, new { status = "Unhealthy", service = "redis", error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckAllAsync()
        {
            var dbResult = await CheckDatabaseAsync() as ObjectResult;
            var redisResult = await CheckRedisAsync() as ObjectResult;

            var isDbHealthy = dbResult?.StatusCode is >= 200 and < 300;
            var isRedisHealthy = redisResult?.StatusCode is >= 200 and < 300;

            var overallHealthy = isDbHealthy && isRedisHealthy;

            return StatusCode(overallHealthy ? 200 : 503, new
            {
                status = overallHealthy ? "Healthy" : "Unhealthy",
                database = dbResult?.Value,
                redis = redisResult?.Value
            });
        }
    }
}



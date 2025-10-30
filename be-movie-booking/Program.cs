using Microsoft.EntityFrameworkCore;
using be_movie_booking.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using DotNetEnv;
using StackExchange.Redis;

//
// === LOAD ENV FILE ===
//
if (Environment.GetEnvironmentVariable("RENDER") == "true")
{
    Console.WriteLine("Running inside Render — using injected environment variables.");
}
else if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true")
{
    Env.Load(".env.docker");
    Console.WriteLine("Loaded .env.docker");
}
else
{
    Env.Load(".env.local");
    Console.WriteLine("Loaded .env.local");
}

//
// === APP BUILDER ===
//
var builder = WebApplication.CreateBuilder(args);

//
// === CONFIG SERVICES ===
//
builder.Services.AddControllers();
builder.Services.AddSignalR();

// 👇 Dùng NSwag để hiển thị Swagger UI (cách mới cho .NET 9)
builder.Services.AddOpenApiDocument(config =>
{
    config.Title = "Movie Booking API";
});

//
// === DATABASE ===
//
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<MovieBookingDbContext>(options =>
    options.UseNpgsql(connectionString));
Console.WriteLine("*****Connection string: " + connectionString);

//
// === REDIS ===
//
var redisConnection = builder.Configuration.GetValue<string>("Redis:Connection");
Console.WriteLine("*****Redis connection: " + redisConnection);
// Đăng ký Redis làm Distributed Cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnection;
});
//khi bạn inject IDistributedCache vào bất kỳ class nào trong ứng dụng,
//ASP.NET Core sẽ cung cấp một instance kết nối đến Redis server mà bạn đã cấu hình.
try
{
    var connection = ConnectionMultiplexer.Connect(redisConnection);
    Console.WriteLine("✅ Redis connected successfully!");
}
catch (RedisConnectionException ex)
{
    Console.WriteLine("❌ Redis connection failed: " + ex.Message);
}

//
// === CORS ===
//
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCors", policy =>
    {
        policy.WithOrigins(
                "https://touchcinema-ta.vercel.app",
                "http://localhost:3000"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

//
// === JWT AUTH ===
//
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwtSection.GetValue<string>("Secret") ?? "dev_secret_change_me";
var issuer = jwtSection.GetValue<string>("Issuer") ?? "be-movie-booking";
var audience = jwtSection.GetValue<string>("Audience") ?? "be-movie-booking-client";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});

//
// === DEPENDENCY INJECTION (SERVICES / REPOSITORIES) ===
//
builder.Services.AddScoped<be_movie_booking.Services.ITokenService, be_movie_booking.Services.TokenService>();
builder.Services.AddScoped<be_movie_booking.Services.IAuthService, be_movie_booking.Services.AuthService>();
builder.Services.AddScoped<be_movie_booking.Services.IUserService, be_movie_booking.Services.UserService>();
builder.Services.AddScoped<be_movie_booking.Services.IMovieService, be_movie_booking.Services.MovieService>();
builder.Services.AddScoped<be_movie_booking.Services.IGenreService, be_movie_booking.Services.GenreService>();
builder.Services.AddScoped<be_movie_booking.Services.ICinemaService, be_movie_booking.Services.CinemaService>();
builder.Services.AddScoped<be_movie_booking.Services.IRoomService, be_movie_booking.Services.RoomService>();
builder.Services.AddScoped<be_movie_booking.Services.ISeatService, be_movie_booking.Services.SeatService>();
builder.Services.AddScoped<be_movie_booking.Services.IPriceRuleService, be_movie_booking.Services.PriceRuleService>();
builder.Services.AddScoped<be_movie_booking.Services.IPricingService, be_movie_booking.Services.PricingService>();
builder.Services.AddScoped<be_movie_booking.Services.IShowtimeService, be_movie_booking.Services.ShowtimeService>();

builder.Services.AddScoped<be_movie_booking.Repositories.IAuthRepository, be_movie_booking.Repositories.AuthRepository>();
builder.Services.AddScoped<be_movie_booking.Repositories.IUserRepository, be_movie_booking.Repositories.UserRepository>();
builder.Services.AddScoped<be_movie_booking.Repositories.IRefreshTokenRepository, be_movie_booking.Repositories.RefreshTokenRepository>();
builder.Services.AddScoped<be_movie_booking.Repositories.IMovieRepository, be_movie_booking.Repositories.MovieRepository>();
builder.Services.AddScoped<be_movie_booking.Repositories.IGenreRepository, be_movie_booking.Repositories.GenreRepository>();
builder.Services.AddScoped<be_movie_booking.Repositories.ICinemaRepository, be_movie_booking.Repositories.CinemaRepository>();
builder.Services.AddScoped<be_movie_booking.Repositories.IRoomRepository, be_movie_booking.Repositories.RoomRepository>();
builder.Services.AddScoped<be_movie_booking.Repositories.ISeatRepository, be_movie_booking.Repositories.SeatRepository>();
builder.Services.AddScoped<be_movie_booking.Repositories.IPriceRuleRepository, be_movie_booking.Repositories.PriceRuleRepository>();
builder.Services.AddScoped<be_movie_booking.Repositories.IShowtimeRepository, be_movie_booking.Repositories.ShowtimeRepository>();

//
// === BUILD APP ===
//
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MovieBookingDbContext>();
    if (dbContext.Database.CanConnect())
    {
        Console.WriteLine("✅ Database connection successful!");
    }
    else
    {
        Console.WriteLine("❌ Database connection failed!");
    }
}

//
// === MIDDLEWARE PIPELINE ===
//
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseOpenApi();    // Tạo /swagger/v1/swagger.json
    app.UseSwaggerUi();  // Hiển thị UI tại /swagger
    Console.WriteLine("Swagger enabled in Development environment");
}

app.UseHttpsRedirection();
app.UseCors("FrontendCors");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<be_movie_booking.Hubs.AppHub>("/hubs/app");

app.Run();

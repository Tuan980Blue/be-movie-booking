using Microsoft.EntityFrameworkCore;
using be_movie_booking.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using DotNetEnv;
using StackExchange.Redis;

// Load config/env
LoadEnvironmentConfig();

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
builder.Services.AddOpenApiDocument(config => { config.Title = "Movie Booking API"; });

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
//tạo một instance của IConnectionMultiplexer
var redis = ConnectionMultiplexer.Connect(redisConnection);
//Nếu kết nối lỗi, nó sẽ ném ra một ngoại lệ
Console.WriteLine($"Connected to Redis server at {redisConnection}");
// Đăng ký IConnectionMultiplexer trong DI container
builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

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
builder.Services.AddScoped<be_movie_booking.Services.ISeatLockService, be_movie_booking.Services.SeatLockService>();
builder.Services.AddScoped<be_movie_booking.Services.IBookingService, be_movie_booking.Services.BookingService>();
builder.Services.AddScoped<be_movie_booking.Services.ITicketService, be_movie_booking.Services.TicketService>();
builder.Services.AddScoped<be_movie_booking.Services.IVnPayService, be_movie_booking.Services.VnPayService>();
builder.Services.AddScoped<be_movie_booking.Services.IPaymentService, be_movie_booking.Services.PaymentService>();

builder.Services
    .AddScoped<be_movie_booking.Repositories.IAuthRepository, be_movie_booking.Repositories.AuthRepository>();
builder.Services
    .AddScoped<be_movie_booking.Repositories.IUserRepository, be_movie_booking.Repositories.UserRepository>();
builder.Services
    .AddScoped<be_movie_booking.Repositories.IRefreshTokenRepository,
        be_movie_booking.Repositories.RefreshTokenRepository>();
builder.Services
    .AddScoped<be_movie_booking.Repositories.IMovieRepository, be_movie_booking.Repositories.MovieRepository>();
builder.Services
    .AddScoped<be_movie_booking.Repositories.IGenreRepository, be_movie_booking.Repositories.GenreRepository>();
builder.Services
    .AddScoped<be_movie_booking.Repositories.ICinemaRepository, be_movie_booking.Repositories.CinemaRepository>();
builder.Services
    .AddScoped<be_movie_booking.Repositories.IRoomRepository, be_movie_booking.Repositories.RoomRepository>();
builder.Services
    .AddScoped<be_movie_booking.Repositories.ISeatRepository, be_movie_booking.Repositories.SeatRepository>();
builder.Services
    .AddScoped<be_movie_booking.Repositories.IPriceRuleRepository, be_movie_booking.Repositories.PriceRuleRepository>();
builder.Services
    .AddScoped<be_movie_booking.Repositories.IShowtimeRepository, be_movie_booking.Repositories.ShowtimeRepository>();
builder.Services
    .AddScoped<be_movie_booking.Repositories.ISeatLockRepository, be_movie_booking.Repositories.SeatLockRepository>();
builder.Services
    .AddScoped<be_movie_booking.Repositories.IBookingRepository, be_movie_booking.Repositories.BookingRepository>();
builder.Services
    .AddScoped<be_movie_booking.Repositories.IBookingDraftRepository, be_movie_booking.Repositories.BookingDraftRepository>();
builder.Services
    .AddScoped<be_movie_booking.Repositories.ITicketRepository, be_movie_booking.Repositories.TicketRepository>();
builder.Services
    .AddScoped<be_movie_booking.Repositories.IPaymentRepository, be_movie_booking.Repositories.PaymentRepository>();

//
// === BUILD APP ===
//
var app = builder.Build();

// Detached explicit database connectivity check from startup. Use health endpoints instead.

//
// === MIDDLEWARE PIPELINE ===
//
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseOpenApi(); // Tạo /swagger/v1/swagger.json
    app.UseSwaggerUi(); // Hiển thị UI tại /swagger
}

app.UseHttpsRedirection();
app.UseCors("FrontendCors");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<be_movie_booking.Hubs.AppHub>("/hubs/app");

app.Run();

static void LoadEnvironmentConfig()
{
    // === LOAD ENV FILE ===
    if (Environment.GetEnvironmentVariable("RENDER") == "true")
    {
        Console.WriteLine("Running inside Render — using injected environment variables.");
        return;
    }

    if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true")
    {
        Env.Load(".env.docker");
        Console.WriteLine("Loaded .env.docker");
        return;
    }

    Env.Load(".env.local");
    Console.WriteLine("Loaded .env.local");
}
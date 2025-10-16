// Import các thư viện cần thiết
using Microsoft.EntityFrameworkCore;  // Entity Framework Core để làm việc với database
using be_movie_booking.Data;         // DbContext của ứng dụng
using Microsoft.AspNetCore.Authentication.JwtBearer;  // JWT Bearer authentication
using Microsoft.IdentityModel.Tokens;  // Token validation
using System.Text;                    // Encoding cho JWT secret

// Tạo WebApplication builder để cấu hình ứng dụng
var builder = WebApplication.CreateBuilder(args);

// ===== CẤU HÌNH SERVICES =====

// Thêm OpenAPI/Swagger để tạo documentation cho API
// Học thêm tại: https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Thêm MVC Controllers để xử lý HTTP requests
builder.Services.AddControllers();

// Cấu hình Entity Framework với PostgreSQL
// đọc connection string từ env
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<MovieBookingDbContext>(options =>
    options.UseNpgsql(connectionString));
Console.WriteLine("Connection string: " + connectionString);

// Cấu hình CORS để cho phép frontend (React, Vue...) truy cập API
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCors", policy =>
    {
        policy.WithOrigins(
                "https://touchcinema-ta.vercel.app",
                "http://localhost:3000"
            )
            .AllowAnyHeader()               // hoặc .WithHeaders("Content-Type","Authorization")
            .WithMethods("GET","POST","PUT","DELETE","OPTIONS")
            .AllowCredentials();            // cần nếu dùng cookie refresh_token
    });
});

// ===== CẤU HÌNH JWT AUTHENTICATION =====

// Lấy cấu hình JWT từ appsettings.json
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwtSection.GetValue<string>("Secret") ?? "dev_secret_change_me";  // Secret key để ký token
var issuer = jwtSection.GetValue<string>("Issuer") ?? "be-movie-booking";         // Người phát hành token
var audience = jwtSection.GetValue<string>("Audience") ?? "be-movie-booking-client"; // Đối tượng sử dụng token

// Cấu hình Authentication với JWT Bearer (ứng dụng sẽ xác thực Bearer Token)
builder.Services.AddAuthentication(options =>
{
    // Đặt JWT Bearer làm scheme mặc định cho authentication và challenge
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
    {
        // Cấu hình các tham số validation cho JWT token
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,           // Kiểm tra issuer có đúng không
            ValidateAudience = true,         // Kiểm tra audience có đúng không
            ValidateLifetime = true,         // Kiểm tra token có hết hạn không
            ValidateIssuerSigningKey = true, // Kiểm tra chữ ký của token
            ValidIssuer = issuer,            // Issuer hợp lệ
            ValidAudience = audience,        // Audience hợp lệ
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)), // Key để verify chữ ký
            ClockSkew = TimeSpan.FromSeconds(30) // Cho phép sai lệch thời gian 30 giây
        };
    });

// ===== ĐĂNG KÝ SERVICES VÀ REPOSITORIES =====

// Ghi chú về DI (Dependency Injection) và AddScoped:
// - DI Container sẽ tạo và "bơm" (inject) các đối tượng bạn yêu cầu qua interface.
// - AddScoped: Mỗi HTTP request sẽ nhận 1 instance MỚI cho mỗi service/repository đã đăng ký.
//   + Trong CÙNG 1 request: tất cả nơi cần cùng service sẽ dùng CHUNG 1 instance.
//   + Sang request KHÁC: tạo instance MỚI (không dùng lại của request trước).
// - Vì DbContext là Scoped, các Service/Repository phụ thuộc DbContext cũng nên Scoped để khớp vòng đời.
// - Lợi ích: quản lý tài nguyên an toàn theo request, dễ test, ít coupling (phụ thuộc abstraction/interface).

// Đăng ký các service với dependency injection
// Scoped: Tạo một instance cho mỗi HTTP request
builder.Services.AddScoped<be_movie_booking.Services.ITokenService, be_movie_booking.Services.TokenService>(); // 1 request -> 1 TokenService
builder.Services.AddScoped<be_movie_booking.Services.IAuthService, be_movie_booking.Services.AuthService>();
builder.Services.AddScoped<be_movie_booking.Services.IUserService, be_movie_booking.Services.UserService>();

// Movie và Genre services
builder.Services.AddScoped<be_movie_booking.Services.IMovieService, be_movie_booking.Services.MovieService>();
builder.Services.AddScoped<be_movie_booking.Services.IGenreService, be_movie_booking.Services.GenreService>();

// Cinema và Room services
builder.Services.AddScoped<be_movie_booking.Services.ICinemaService, be_movie_booking.Services.CinemaService>();
builder.Services.AddScoped<be_movie_booking.Services.IRoomService, be_movie_booking.Services.RoomService>();
builder.Services.AddScoped<be_movie_booking.Services.ISeatService, be_movie_booking.Services.SeatService>();

// PriceRule services
builder.Services.AddScoped<be_movie_booking.Services.IPriceRuleService, be_movie_booking.Services.PriceRuleService>();
builder.Services.AddScoped<be_movie_booking.Services.IPricingService, be_movie_booking.Services.PricingService>();

// Showtime services
builder.Services.AddScoped<be_movie_booking.Services.IShowtimeService, be_movie_booking.Services.ShowtimeService>();

// Đăng ký các repository để truy cập database
builder.Services.AddScoped<be_movie_booking.Repositories.IAuthRepository, be_movie_booking.Repositories.AuthRepository>();   // Repo cũng Scoped để dùng chung DbContext trong 1 request
builder.Services.AddScoped<be_movie_booking.Repositories.IUserRepository, be_movie_booking.Repositories.UserRepository>();
builder.Services.AddScoped<be_movie_booking.Repositories.IRefreshTokenRepository, be_movie_booking.Repositories.RefreshTokenRepository>();

// Movie và Genre repositories
builder.Services.AddScoped<be_movie_booking.Repositories.IMovieRepository, be_movie_booking.Repositories.MovieRepository>();
builder.Services.AddScoped<be_movie_booking.Repositories.IGenreRepository, be_movie_booking.Repositories.GenreRepository>();

// Cinema và Room repositories
builder.Services.AddScoped<be_movie_booking.Repositories.ICinemaRepository, be_movie_booking.Repositories.CinemaRepository>();
builder.Services.AddScoped<be_movie_booking.Repositories.IRoomRepository, be_movie_booking.Repositories.RoomRepository>();
builder.Services.AddScoped<be_movie_booking.Repositories.ISeatRepository, be_movie_booking.Repositories.SeatRepository>();

// PriceRule repository
builder.Services.AddScoped<be_movie_booking.Repositories.IPriceRuleRepository, be_movie_booking.Repositories.PriceRuleRepository>();

// Showtime repository
builder.Services.AddScoped<be_movie_booking.Repositories.IShowtimeRepository, be_movie_booking.Repositories.ShowtimeRepository>();


// Build ứng dụng từ builder
var app = builder.Build();

// ===== CẤU HÌNH HTTP REQUEST PIPELINE =====

// Chỉ enable OpenAPI/Swagger trong môi trường Development
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // Tạo endpoint cho OpenAPI specification
}

// Redirect HTTP requests sang HTTPS để bảo mật
app.UseHttpsRedirection();

// Bật CORS TRƯỚC UseAuthentication/UseAuthorization
app.UseCors("FrontendCors");

// Middleware xác thực - phải được đặt trước UseAuthorization
// Khi request đến, Authentication middleware (được thêm bằng app.UseAuthentication()) sẽ:
// Tìm header Authorization.
// Kiểm tra xem có dạng Bearer <token> hay không.
// Nếu có, nó sẽ giải mã và xác thực JWT (chữ ký, hạn dùng…).
// Nếu hợp lệ, nó tạo một ClaimsPrincipal (chứa các Claims) và gắn vào HttpContext.User.
app.UseAuthentication();

// Middleware phân quyền - kiểm tra quyền truy cập
app.UseAuthorization();

// Map các controller endpoints
app.MapControllers();

// Chạy ứng dụng
app.Run();

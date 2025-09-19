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
// Sử dụng connection string từ appsettings.json
builder.Services.AddDbContext<MovieBookingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ===== CẤU HÌNH JWT AUTHENTICATION =====

// Lấy cấu hình JWT từ appsettings.json
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwtSection.GetValue<string>("Secret") ?? "dev_secret_change_me";  // Secret key để ký token
var issuer = jwtSection.GetValue<string>("Issuer") ?? "be-movie-booking";         // Người phát hành token
var audience = jwtSection.GetValue<string>("Audience") ?? "be-movie-booking-client"; // Đối tượng sử dụng token

// Cấu hình Authentication với JWT Bearer
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

// Đăng ký các service với dependency injection
// Scoped: Tạo một instance cho mỗi HTTP request
builder.Services.AddScoped<be_movie_booking.Services.ITokenService, be_movie_booking.Services.TokenService>();
builder.Services.AddScoped<be_movie_booking.Services.IAuthService, be_movie_booking.Services.AuthService>();

// Đăng ký các repository để truy cập database
builder.Services.AddScoped<be_movie_booking.Repositories.IUserRepository, be_movie_booking.Repositories.UserRepository>();
builder.Services.AddScoped<be_movie_booking.Repositories.IRefreshTokenRepository, be_movie_booking.Repositories.RefreshTokenRepository>();

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

// Middleware xác thực - phải được đặt trước UseAuthorization
app.UseAuthentication();

// Middleware phân quyền - kiểm tra quyền truy cập
app.UseAuthorization();

// Map các controller endpoints
app.MapControllers();

// Chạy ứng dụng
app.Run();

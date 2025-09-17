using Microsoft.EntityFrameworkCore;
using be_movie_booking.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add controllers
builder.Services.AddControllers();

// Add Entity Framework với PostgreSQL
builder.Services.AddDbContext<MovieBookingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT configuration
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
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
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

// Token & Auth services
builder.Services.AddScoped<be_movie_booking.Services.ITokenService, be_movie_booking.Services.TokenService>();
builder.Services.AddScoped<be_movie_booking.Services.IAuthService, be_movie_booking.Services.AuthService>();

// Repositories
builder.Services.AddScoped<be_movie_booking.Repositories.IUserRepository, be_movie_booking.Repositories.UserRepository>();
builder.Services.AddScoped<be_movie_booking.Repositories.IRefreshTokenRepository, be_movie_booking.Repositories.RefreshTokenRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

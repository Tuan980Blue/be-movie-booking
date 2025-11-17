using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using DotNetEnv;

namespace be_movie_booking.Data
{
    public class MovieBookingDbContextFactory : IDesignTimeDbContextFactory<MovieBookingDbContext>
    {
        public MovieBookingDbContext CreateDbContext(string[] args)
        {
            // Load environment variables từ .env.local
            if (Environment.GetEnvironmentVariable("RENDER") != "true" 
                && Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != "true")
            {
                try
                {
                    Env.Load(".env.local");
                }
                catch
                {
                    // Ignore if file doesn't exist
                }
            }

            var optionsBuilder = new DbContextOptionsBuilder<MovieBookingDbContext>();

            // Lấy connection string từ biến môi trường hoặc sử dụng mặc định
            var connStr = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
            if (string.IsNullOrEmpty(connStr))
            {
                // Sử dụng connection string mặc định cho việc tạo migration (không cần kết nối thực sự)
                connStr = "Host=localhost;Database=movie_booking;Username=postgres;Password=postgres";
            }

            optionsBuilder.UseNpgsql(connStr);

            return new MovieBookingDbContext(optionsBuilder.Options);
        }
    }
}
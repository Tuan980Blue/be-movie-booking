using Microsoft.EntityFrameworkCore;
using be_movie_booking.Models;

namespace be_movie_booking.Data;

public class MovieBookingDbContext : DbContext
{
    public MovieBookingDbContext(DbContextOptions<MovieBookingDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<Cinema> Cinemas => Set<Cinema>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Seat> Seats => Set<Seat>();

    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<MovieGenre> MovieGenres => Set<MovieGenre>();

    public DbSet<Showtime> Showtimes => Set<Showtime>();
    public DbSet<PriceRule> PriceRules => Set<PriceRule>();

    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingItem> BookingItems => Set<BookingItem>();
    public DbSet<Ticket> Tickets => Set<Ticket>();

    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentEvent> PaymentEvents => Set<PaymentEvent>();

    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<PromotionUsage> PromotionUsages => Set<PromotionUsage>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordReset> PasswordResets => Set<PasswordReset>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Users
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.Phone).IsUnique();
        });

        // Roles
        modelBuilder.Entity<Role>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
            
            // Seed default roles
            e.HasData(
                new Role { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "User" },
                new Role { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Admin" },
                new Role { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Manager" }
            );
        });
        modelBuilder.Entity<UserRole>(e =>
        {
            e.HasKey(x => new { x.UserId, x.RoleId });
            e.HasOne(x => x.User).WithMany(u => u.UserRoles).HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Role).WithMany(r => r.UserRoles).HasForeignKey(x => x.RoleId);
        });

        // Cinemas / Rooms / Seats
        modelBuilder.Entity<Cinema>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired();
        });
        modelBuilder.Entity<Room>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.CinemaId, x.Code }).IsUnique();
            e.HasOne(x => x.Cinema).WithMany(c => c.Rooms).HasForeignKey(x => x.CinemaId);
        });
        modelBuilder.Entity<Seat>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Room).WithMany(r => r.Seats).HasForeignKey(x => x.RoomId);
            e.HasIndex(x => new { x.RoomId, x.RowLabel, x.SeatNumber }).IsUnique();
            
            // Seat properties
            e.Property(x => x.RowLabel).IsRequired().HasMaxLength(10);
            e.Property(x => x.SeatNumber).IsRequired();
            e.Property(x => x.SeatType).IsRequired();
            e.Property(x => x.IsActive).IsRequired();
            
            // Layout properties
            e.Property(x => x.PositionX);
            e.Property(x => x.PositionY);
            
            // Special notes
            e.Property(x => x.SpecialNotes).HasMaxLength(500);
            
            // Audit properties
            e.Property(x => x.CreatedAt).IsRequired();
            e.Property(x => x.UpdatedAt);
            e.Property(x => x.CreatedBy);
            e.Property(x => x.UpdatedBy);
        });

        // Movies / Genres
        modelBuilder.Entity<Movie>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).IsRequired();
        });
        modelBuilder.Entity<Genre>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Name).IsUnique();
        });
        modelBuilder.Entity<MovieGenre>(e =>
        {
            e.HasKey(x => new { x.MovieId, x.GenreId });
            e.HasOne(x => x.Movie).WithMany(m => m.MovieGenres).HasForeignKey(x => x.MovieId);
            e.HasOne(x => x.Genre).WithMany(g => g.MovieGenres).HasForeignKey(x => x.GenreId);
        });

        // Showtimes
        modelBuilder.Entity<Showtime>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Movie).WithMany().HasForeignKey(x => x.MovieId);
            e.HasOne(x => x.Room).WithMany().HasForeignKey(x => x.RoomId);
            e.HasIndex(x => new { x.RoomId, x.StartUtc });
            e.HasIndex(x => new { x.MovieId, x.StartUtc });
        });
        modelBuilder.Entity<PriceRule>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Cinema).WithMany().HasForeignKey(x => x.CinemaId);
            e.HasIndex(x => new { x.CinemaId, x.DayType, x.SeatType }).IsUnique();
            // Đảm bảo Global (CinemaId IS NULL) chỉ có 1 bản ghi cho mỗi (DayType, SeatType)
            e.HasIndex(x => new { x.DayType, x.SeatType })
                .IsUnique()
                .HasFilter("\"CinemaId\" IS NULL");
            e.Property(x => x.PriceMinor).IsRequired();
        });

        // Bookings
        modelBuilder.Entity<Booking>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });
        modelBuilder.Entity<BookingItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Booking).WithMany(b => b.Items).HasForeignKey(x => x.BookingId);
            e.HasOne(x => x.Showtime).WithMany().HasForeignKey(x => x.ShowtimeId);
            e.HasOne(x => x.Seat).WithMany().HasForeignKey(x => x.SeatId);
            e.HasIndex(x => new { x.ShowtimeId, x.SeatId });
        });
        modelBuilder.Entity<Ticket>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TicketCode).IsRequired();
            e.HasIndex(x => x.TicketCode).IsUnique();
            e.HasOne(x => x.Booking).WithMany(b => b.Tickets).HasForeignKey(x => x.BookingId);
            e.HasOne(x => x.Showtime).WithMany().HasForeignKey(x => x.ShowtimeId);
            e.HasOne(x => x.Seat).WithMany().HasForeignKey(x => x.SeatId);
        });

        // Payments
        modelBuilder.Entity<Payment>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Booking).WithMany().HasForeignKey(x => x.BookingId);
            e.HasIndex(x => new { x.BookingId, x.Status });
        });
        modelBuilder.Entity<PaymentEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Payment).WithMany(p => p.Events).HasForeignKey(x => x.PaymentId);
        });

        // Promotions
        modelBuilder.Entity<Promotion>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Code).IsUnique();
        });
        modelBuilder.Entity<PromotionUsage>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Promotion).WithMany().HasForeignKey(x => x.PromotionId);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Booking).WithMany().HasForeignKey(x => x.BookingId);
            e.HasIndex(x => new { x.PromotionId, x.BookingId }).IsUnique();
        });

        // Security
        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.User).WithMany(u => u.RefreshTokens).HasForeignKey(x => x.UserId);
            e.HasIndex(x => x.TokenHash).IsUnique();
        });
        modelBuilder.Entity<PasswordReset>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            e.HasIndex(x => x.TokenHash).IsUnique();
        });
    }
}

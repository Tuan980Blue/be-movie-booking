namespace be_movie_booking.Models;

public enum MovieStatus
{
    Draft = 0,
    ComingSoon = 1,
    NowShowing = 2,
    Archived = 3
}

public class Genre
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;

    public ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
}

public class Movie
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string? OriginalTitle { get; set; }
    public int DurationMinutes { get; set; }
    public string? Rated { get; set; }
    public string? Description { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public string? PosterUrl { get; set; }
    public string? BackdropUrl { get; set; }
    public MovieStatus Status { get; set; } = MovieStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
}

public class MovieGenre
{
    public Guid MovieId { get; set; }
    public Movie Movie { get; set; } = null!;

    public Guid GenreId { get; set; }
    public Genre Genre { get; set; } = null!;
}

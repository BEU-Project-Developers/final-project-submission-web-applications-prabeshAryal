using Microsoft.EntityFrameworkCore;
using MusicApp.Models;

namespace MusicApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<UserFollower> UserFollowers { get; set; }
        public DbSet<UserFavorite> UserFavorites { get; set; }
        public DbSet<Artist> Artists { get; set; }
        public DbSet<Album> Albums { get; set; }
        public DbSet<Song> Songs { get; set; }
        public DbSet<Playlist> Playlists { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // Configure UserRole
            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);

            // Configure UserFollower
            modelBuilder.Entity<UserFollower>()
                .HasKey(uf => new { uf.FollowerId, uf.FollowingId });

            modelBuilder.Entity<UserFollower>()
                .HasOne(uf => uf.Follower)
                .WithMany(u => u.Followers)
                .HasForeignKey(uf => uf.FollowerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserFollower>()
                .HasOne(uf => uf.Following)
                .WithMany(u => u.Following)
                .HasForeignKey(uf => uf.FollowingId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure UserFavorite
            modelBuilder.Entity<UserFavorite>()
                .HasOne(uf => uf.User)
                .WithMany(u => u.Favorites)
                .HasForeignKey(uf => uf.UserId);

            // Seed default roles
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Admin", Description = "Administrator role" },
                new Role { Id = 2, Name = "User", Description = "Regular user role" }
            );

            // Seed admin user
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    FirstName = "Admin",
                    LastName = "User",
                    Username = "admin",
                    Email = "admin@musicapp.com",
                    PasswordHash = "admin123", // In real app, use proper password hashing
                    ProfileImageUrl = "https://picsum.photos/200/200?random=1",
                    CreatedAt = DateTime.UtcNow
                }
            );

            // Assign admin role to admin user
            modelBuilder.Entity<UserRole>().HasData(
                new UserRole { Id = 1, UserId = 1, RoleId = 1 }
            );

            // Seed sample artists
            modelBuilder.Entity<Artist>().HasData(
                new Artist
                {
                    Id = 1,
                    Name = "Sample Artist 1",
                    Bio = "A talented musician with a unique style",
                    ImageUrl = "https://picsum.photos/200/200?random=2",
                    Country = "United States",
                    Genre = "Pop/Rock",
                    FormedDate = new DateTime(2010, 3, 15),
                    MonthlyListeners = 1500000,
                    CreatedAt = DateTime.UtcNow
                },
                new Artist
                {
                    Id = 2,
                    Name = "Sample Artist 2",
                    Bio = "An innovative artist pushing boundaries",
                    ImageUrl = "https://picsum.photos/200/200?random=3",
                    Country = "United Kingdom",
                    Genre = "Alternative",
                    FormedDate = new DateTime(2012, 7, 23),
                    MonthlyListeners = 2000000,
                    CreatedAt = DateTime.UtcNow
                }
            );

            // Seed sample albums
            modelBuilder.Entity<Album>().HasData(
                new Album
                {
                    Id = 1,
                    Title = "First Album",
                    ArtistId = 1,
                    CoverImageUrl = "https://picsum.photos/200/200?random=4",
                    Year = 2023,
                    Description = "The debut album with amazing tracks",
                    Genre = "Pop",
                    ReleaseDate = new DateTime(2023, 1, 15),
                    TotalTracks = 10,
                    Duration = TimeSpan.FromMinutes(45),
                    CreatedAt = DateTime.UtcNow
                },
                new Album
                {
                    Id = 2,
                    Title = "Second Album",
                    ArtistId = 2,
                    CoverImageUrl = "https://picsum.photos/200/200?random=5",
                    Year = 2024,
                    Description = "The sophomore album that broke records",
                    Genre = "Rock",
                    ReleaseDate = new DateTime(2024, 3, 22),
                    TotalTracks = 12,
                    Duration = TimeSpan.FromMinutes(52),
                    CreatedAt = DateTime.UtcNow
                }
            );

            // Seed sample songs
            modelBuilder.Entity<Song>().HasData(
                new Song
                {
                    Id = 1,
                    Title = "Song 1",
                    ArtistId = 1,
                    AlbumId = 1,
                    Duration = TimeSpan.FromMinutes(3.75), // 3:45
                    AudioUrl = "~/assets/audio/song1.mp3",
                    CoverImageUrl = "https://picsum.photos/200/200?random=8",
                    TrackNumber = 1,
                    Genre = "Pop",
                    ReleaseDate = new DateTime(2023, 1, 15),
                    PlayCount = 10000,
                    CreatedAt = DateTime.UtcNow
                },
                new Song
                {
                    Id = 2,
                    Title = "Song 2",
                    ArtistId = 2,
                    AlbumId = 2,
                    Duration = TimeSpan.FromMinutes(4.33), // 4:20
                    AudioUrl = "~/assets/audio/song2.mp3",
                    CoverImageUrl = "https://picsum.photos/200/200?random=9",
                    TrackNumber = 1,
                    Genre = "Rock",
                    ReleaseDate = new DateTime(2024, 3, 22),
                    PlayCount = 8500,
                    CreatedAt = DateTime.UtcNow
                }
            );

            // Seed sample playlists
            modelBuilder.Entity<Playlist>().HasData(
                new Playlist
                {
                    Id = 1,
                    Name = "My Favorites",
                    Description = "A collection of my favorite songs",
                    UserId = 1,
                    CoverImageUrl = "https://picsum.photos/200/200?random=6",
                    IsPublic = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Playlist
                {
                    Id = 2,
                    Name = "Workout Mix",
                    Description = "Perfect for your workout session",
                    UserId = 1,
                    CoverImageUrl = "https://picsum.photos/200/200?random=7",
                    IsPublic = true,
                    CreatedAt = DateTime.UtcNow
                }
            );
        }
    }
} 
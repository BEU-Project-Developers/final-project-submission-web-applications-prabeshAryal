// Data/MusicDbContext.cs
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using MusicAppBackend.Models;

namespace MusicAppBackend.Data
{
    public class MusicDbContext : DbContext
    {
        public MusicDbContext(DbContextOptions<MusicDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<UserRole> UserRoles { get; set; } = null!;
        public DbSet<UserFollower> UserFollowers { get; set; } = null!;
        public DbSet<UserFavorite> UserFavorites { get; set; } = null!;
        public DbSet<Artist> Artists { get; set; } = null!;
        public DbSet<Album> Albums { get; set; } = null!;
        public DbSet<Song> Songs { get; set; } = null!;        public DbSet<Playlist> Playlists { get; set; } = null!;
        public DbSet<PlaylistSong> PlaylistSongs { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
        public DbSet<UserSongPlay> UserSongPlays { get; set; } = null!;
        public DbSet<AlbumLike> AlbumLikes { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure User-Role (many-to-many)
            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure User-Followers (self-referencing many-to-many)
            modelBuilder.Entity<UserFollower>()
                .HasKey(uf => new { uf.UserId, uf.FollowerId });

            modelBuilder.Entity<UserFollower>()
                .HasOne(uf => uf.User)
                .WithMany(u => u.Followers)
                .HasForeignKey(uf => uf.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserFollower>()
                .HasOne(uf => uf.Follower)
                .WithMany(u => u.Following)
                .HasForeignKey(uf => uf.FollowerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure User-Favorites (many-to-many with Songs)
            modelBuilder.Entity<UserFavorite>()
                .HasKey(uf => new { uf.UserId, uf.SongId });

            modelBuilder.Entity<UserFavorite>()
                .HasOne(uf => uf.User)
                .WithMany(u => u.Favorites)
                .HasForeignKey(uf => uf.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserFavorite>()
                .HasOne(uf => uf.Song)
                .WithMany(s => s.UserFavorites)
                .HasForeignKey(uf => uf.SongId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Artist-Albums (one-to-many)
            modelBuilder.Entity<Album>()
                .HasOne(a => a.Artist)
                .WithMany(a => a.Albums)
                .HasForeignKey(a => a.ArtistId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);  // ArtistId is nullable

            // Configure Album-Songs (one-to-many)
            modelBuilder.Entity<Song>()
                .HasOne(s => s.Album)
                .WithMany(a => a.Songs)
                .HasForeignKey(s => s.AlbumId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);  // AlbumId is nullable

            // Configure Song-Artist (many-to-one)
            modelBuilder.Entity<Song>()
                .HasOne(s => s.Artist)
                .WithMany()
                .HasForeignKey(s => s.ArtistId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);  // ArtistId is nullable

            // Configure User-Playlists (one-to-many)
            modelBuilder.Entity<Playlist>()
                .HasOne(p => p.User)
                .WithMany(u => u.Playlists)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Playlist-Songs (many-to-many)
            modelBuilder.Entity<PlaylistSong>()
                .HasKey(ps => new { ps.PlaylistId, ps.SongId });

            modelBuilder.Entity<PlaylistSong>()
                .HasOne(ps => ps.Playlist)
                .WithMany(p => p.PlaylistSongs)
                .HasForeignKey(ps => ps.PlaylistId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PlaylistSong>()
                .HasOne(ps => ps.Song)
                .WithMany(s => s.PlaylistSongs)
                .HasForeignKey(ps => ps.SongId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure many-to-many relationship between Playlists and Songs
            modelBuilder.Entity<Playlist>()
                .HasMany(p => p.Songs)
                .WithMany(s => s.Playlists)
                .UsingEntity<PlaylistSong>();

            // Configure RefreshToken-User (one-to-many)
            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany()
                .HasForeignKey(rt => rt.UserId)                .OnDelete(DeleteBehavior.Cascade);

            // Configure UserSongPlay
            modelBuilder.Entity<UserSongPlay>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<UserSongPlay>()
                .HasOne(x => x.Song)
                .WithMany()
                .HasForeignKey(x => x.SongId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
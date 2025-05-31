using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MusicAppBackend.Models
{
    public class Song
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;
          public int? ArtistId { get; set; } // Keep for backward compatibility
        
        public int? AlbumId { get; set; }
        
        public TimeSpan Duration { get; set; }
        
        public string? AudioUrl { get; set; }
        
        public string? CoverImageUrl { get; set; }
        
        public int? TrackNumber { get; set; }
        
        [StringLength(50)]
        public string? Genre { get; set; }
        
        public DateTime? ReleaseDate { get; set; }
        
        public int PlayCount { get; set; } = 0;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual Artist? Artist { get; set; } // Keep for backward compatibility
        
        public virtual Album? Album { get; set; }
        
        public virtual ICollection<Playlist> Playlists { get; set; } = new List<Playlist>();
        
        public virtual ICollection<PlaylistSong> PlaylistSongs { get; set; } = new List<PlaylistSong>();
        
        public virtual ICollection<UserFavorite> UserFavorites { get; set; } = new List<UserFavorite>();

        // New many-to-many relationship
        public virtual ICollection<SongArtist> SongArtists { get; set; } = new List<SongArtist>();
    }
} 
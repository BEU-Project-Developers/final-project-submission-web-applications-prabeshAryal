using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace MusicApp.Models
{
    public class Playlist
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public string CoverImageUrl { get; set; }

        public bool IsPublic { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public int UserId { get; set; }
        public User User { get; set; }
        public ICollection<Song> Songs { get; set; }

        // Computed properties
        public int SongCount => Songs?.Count ?? 0;
        public TimeSpan TotalDuration => Songs?.Aggregate(TimeSpan.Zero, (total, song) => total + song.Duration) ?? TimeSpan.Zero;
    }
}

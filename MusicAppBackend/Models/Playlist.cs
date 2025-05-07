using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MusicAppBackend.Models
{
    public class Playlist
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        public int UserId { get; set; }
        
        public string? CoverImageUrl { get; set; }
        
        public bool IsPublic { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual User User { get; set; } = null!;
        
        public virtual ICollection<Song> Songs { get; set; } = new List<Song>();
        
        public virtual ICollection<PlaylistSong> PlaylistSongs { get; set; } = new List<PlaylistSong>();
    }
} 
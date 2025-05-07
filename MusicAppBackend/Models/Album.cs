using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MusicAppBackend.Models
{
    public class Album
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;
        
        public int ArtistId { get; set; }
        
        public string? CoverImageUrl { get; set; }
        
        public int? Year { get; set; }
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        [StringLength(50)]
        public string? Genre { get; set; }
        
        public DateTime? ReleaseDate { get; set; }
        
        public int? TotalTracks { get; set; }
        
        public TimeSpan? Duration { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual Artist Artist { get; set; } = null!;
        
        public virtual ICollection<Song> Songs { get; set; } = new List<Song>();
    }
} 
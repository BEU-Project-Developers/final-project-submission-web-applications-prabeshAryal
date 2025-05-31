using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MusicAppBackend.Models
{
    public class Artist
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? Bio { get; set; }
        
        public string? ImageUrl { get; set; }
        
        [StringLength(100)]
        public string? Country { get; set; }
        
        [StringLength(50)]
        public string? Genre { get; set; }
        
        public DateTime? FormedDate { get; set; }
        
        public int? MonthlyListeners { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
          // Navigation properties
        public virtual ICollection<Album> Albums { get; set; } = new List<Album>();
        
        // New many-to-many relationship
        public virtual ICollection<SongArtist> SongArtists { get; set; } = new List<SongArtist>();
    }

    public class ArtistUpdateDTO
    {
        public string? Name { get; set; }
        public string? Bio { get; set; }
        public string? Country { get; set; }
        public string? Genre { get; set; }
        public DateTime? FormedDate { get; set; }
        public int? MonthlyListeners { get; set; }
        public bool? IsActive { get; set; }
    }
}
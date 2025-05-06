using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MusicApp.Models
{
    public class Album
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [Required]
        public int ArtistId { get; set; }
        public Artist Artist { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; }

        public string CoverImageUrl { get; set; }

        public DateTime ReleaseDate { get; set; }
        
        [Required]
        public string Genre { get; set; }
        
        public int TotalTracks { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual ICollection<Song> Songs { get; set; }
    }
}

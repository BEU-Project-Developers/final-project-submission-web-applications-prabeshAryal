using System;
using System.Collections.Generic;

namespace MusicApp.Models
{
    public class Album
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int ArtistId { get; set; }
        public Artist Artist { get; set; }
        public int Year { get; set; }
        public string Description { get; set; }
        public string CoverImageUrl { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string Genre { get; set; }
        public int TotalTracks { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual ICollection<Song> Songs { get; set; }
    }
}

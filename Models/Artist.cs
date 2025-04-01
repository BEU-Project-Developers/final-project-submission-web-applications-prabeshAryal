// MusicApp.Models/Artist.cs
namespace MusicApp.Models
{
    public class Artist
    {
        public int Id { get; set; } // Keep Id if you are using it for details page links
        public string Name { get; set; }
        public string Bio { get; set; } // You might or might not use Bio in the Index view
        public string ImageUrl { get; set; }
        public string Genre { get; set; } // Add Genre property
        public int AlbumCount { get; set; } // Add AlbumCount property
    }
}
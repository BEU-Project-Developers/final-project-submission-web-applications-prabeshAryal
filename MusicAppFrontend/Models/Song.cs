using System;
using System.Collections.Generic;

namespace MusicApp.Models
{
    public class Song
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string AudioUrl { get; set; }
        public string CoverImageUrl { get; set; }
        public TimeSpan Duration { get; set; }
        public int TrackNumber { get; set; }
        public string Genre { get; set; }
        public DateTime ReleaseDate { get; set; }
        public int PlayCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        // Navigation properties
        public int AlbumId { get; set; }
        public virtual Album Album { get; set; }
        public int ArtistId { get; set; }
        public virtual Artist Artist { get; set; }
        public virtual ICollection<Playlist> Playlists { get; set; }
        public virtual ICollection<User> FavoritedBy { get; set; }
    }
}

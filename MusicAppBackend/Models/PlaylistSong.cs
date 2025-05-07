using System;

namespace MusicAppBackend.Models
{
    public class PlaylistSong
    {
        public int PlaylistId { get; set; }
        public int SongId { get; set; }
        public int Order { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual Playlist Playlist { get; set; } = null!;
        public virtual Song Song { get; set; } = null!;
    }
} 
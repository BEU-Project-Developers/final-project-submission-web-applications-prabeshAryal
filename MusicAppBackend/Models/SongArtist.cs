using System.ComponentModel.DataAnnotations;

namespace MusicAppBackend.Models
{
    public class SongArtist
    {
        public int SongId { get; set; }
        public int ArtistId { get; set; }
        public bool IsPrimaryArtist { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Song Song { get; set; } = null!;
        public virtual Artist Artist { get; set; } = null!;
    }
}

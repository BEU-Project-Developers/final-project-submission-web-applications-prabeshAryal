using System;

namespace MusicAppBackend.Models
{
    public class UserSongPlay
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int SongId { get; set; }
        public DateTime PlayedAt { get; set; } = DateTime.UtcNow;
        public TimeSpan? Duration { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Song Song { get; set; } = null!;
    }
}

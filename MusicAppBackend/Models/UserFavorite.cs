using System;

namespace MusicAppBackend.Models
{
    public class UserFavorite
    {
        public int UserId { get; set; }
        public int SongId { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Song Song { get; set; } = null!;
    }
} 
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicAppBackend.Models
{
    public class AlbumLike
    {
        public int Id { get; set; }

        // Foreign key for User
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        // Foreign key for Album
        public int AlbumId { get; set; }
        [ForeignKey("AlbumId")]
        public virtual Album? Album { get; set; }

        public DateTime LikedAt { get; set; } = DateTime.UtcNow;
    }
}

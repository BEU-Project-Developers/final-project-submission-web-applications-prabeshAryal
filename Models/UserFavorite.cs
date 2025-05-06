using System.ComponentModel.DataAnnotations;

namespace MusicApp.Models
{
    public class UserFavorite
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required]
        public int ContentId { get; set; }

        [Required]
        [StringLength(50)]
        public string ContentType { get; set; } // "Song", "Album", "Artist", "Playlist"

        public System.DateTime CreatedAt { get; set; }
    }
} 
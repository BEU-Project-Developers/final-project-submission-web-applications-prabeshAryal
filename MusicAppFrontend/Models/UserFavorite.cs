using System.ComponentModel.DataAnnotations;

namespace MusicApp.Models
{
    public class UserFavorite
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public int ContentId { get; set; }
        public string ContentType { get; set; } = string.Empty; // "Song", "Album", "Artist", "Playlist"
        public System.DateTime CreatedAt { get; set; }
    }
}
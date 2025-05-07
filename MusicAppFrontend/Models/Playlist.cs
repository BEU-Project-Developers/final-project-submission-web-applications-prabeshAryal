using System;
using System.Collections.Generic;

namespace MusicApp.Models
{
    public class Playlist
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string CoverImageUrl { get; set; }
        public bool IsPublic { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        // Navigation properties
        public int UserId { get; set; }
        public User User { get; set; }
        public ICollection<Song> Songs { get; set; }
    }
}

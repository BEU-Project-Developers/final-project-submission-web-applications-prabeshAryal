// MusicApp.Models/Artist.cs
using System;
using System.Collections.Generic;

namespace MusicApp.Models
{
    public class Artist
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? ImageUrl { get; set; }
        public string? Country { get; set; }
        public string? Genre { get; set; }
        public DateTime? FormedDate { get; set; }
        public int? MonthlyListeners { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual ICollection<Album> Albums { get; set; } = new List<Album>();
        public virtual ICollection<Song> Songs { get; set; } = new List<Song>();
        public virtual ICollection<User> Followers { get; set; } = new List<User>();
    }
}
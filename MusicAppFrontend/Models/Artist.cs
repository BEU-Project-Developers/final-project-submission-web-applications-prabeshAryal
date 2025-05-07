// MusicApp.Models/Artist.cs
using System;
using System.Collections.Generic;

namespace MusicApp.Models
{
    public class Artist
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Bio { get; set; }
        public string ImageUrl { get; set; }
        public string Genre { get; set; }
        public DateTime FormedDate { get; set; }
        public string Country { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int MonthlyListeners { get; set; }
        // Navigation properties
        public ICollection<Album> Albums { get; set; }
        public ICollection<Song> Songs { get; set; }
        public ICollection<User> Followers { get; set; }
    }
}
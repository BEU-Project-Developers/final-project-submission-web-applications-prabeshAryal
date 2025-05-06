// MusicApp.Models/Artist.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace MusicApp.Models
{
    public class Artist
    {
        public int Id { get; set; } // Keep Id if you are using it for details page links

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Bio { get; set; }

        public string ImageUrl { get; set; }

        public string Genre { get; set; }

        public DateTime FormedDate { get; set; }

        [Required]
        public string Country { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int MonthlyListeners { get; set; }
        
        // Navigation properties
        public ICollection<Album> Albums { get; set; }
        public ICollection<Song> Songs { get; set; }
        public ICollection<User> Followers { get; set; }

        // Computed properties
        public int AlbumCount => Albums?.Count ?? 0;
        public int SongCount => Songs?.Count ?? 0;
        public int FollowerCount => Followers?.Count ?? 0;
    }
}
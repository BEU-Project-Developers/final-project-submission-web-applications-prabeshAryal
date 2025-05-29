using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MusicApp.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [Required]
        [StringLength(100)]
        public string PasswordHash { get; set; }

        public string ProfileImageUrl { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        // Navigation properties
        public ICollection<Playlist> Playlists { get; set; } = new List<Playlist>();
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<UserFollower> Followers { get; set; } = new List<UserFollower>();
        public ICollection<UserFollower> Following { get; set; } = new List<UserFollower>();
        public ICollection<UserFavorite> Favorites { get; set; } = new List<UserFavorite>();
    }
} 
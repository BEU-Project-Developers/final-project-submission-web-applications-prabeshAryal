using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MusicAppBackend.Models
{
    public class User
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;
        
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }
        
        [StringLength(1000)]
        public string? Bio { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        
        public virtual ICollection<Playlist> Playlists { get; set; } = new List<Playlist>();
        
        public virtual ICollection<UserFollower> Followers { get; set; } = new List<UserFollower>();
        
        public virtual ICollection<UserFollower> Following { get; set; } = new List<UserFollower>();
        
        public virtual ICollection<UserFavorite> Favorites { get; set; } = new List<UserFavorite>();
    }
} 
using System;
using System.ComponentModel.DataAnnotations;

namespace MusicAppBackend.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }
        
        [Required]
        public string Token { get; set; } = string.Empty;
        
        public int UserId { get; set; }
        
        public DateTime ExpiryDate { get; set; }
        
        public DateTime IssuedAt { get; set; }
        
        public bool IsRevoked { get; set; }
        
        // Navigation properties
        public virtual User User { get; set; } = null!;
    }
} 
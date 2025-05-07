using System;

namespace MusicAppBackend.Models
{
    public class UserFollower
    {
        public int UserId { get; set; }
        public int FollowerId { get; set; }
        public DateTime FollowedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual User Follower { get; set; } = null!;
    }
} 
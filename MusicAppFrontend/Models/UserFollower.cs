using System.ComponentModel.DataAnnotations;

namespace MusicApp.Models
{
    public class UserFollower
    {
        public int Id { get; set; }
        public int FollowerId { get; set; }
        public User Follower { get; set; }
        public int FollowingId { get; set; }
        public User Following { get; set; }
        public System.DateTime CreatedAt { get; set; }
    }
}
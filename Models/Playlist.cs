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
        public List<int> SongIds { get; set; } = new List<int>();
    }
}

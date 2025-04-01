namespace MusicApp.Models
{
    public class Song
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int AlbumId { get; set; }
        public int ArtistId { get; set; }
        public int Duration { get; set; } // in seconds
        public string AudioUrl { get; set; }
    }
}

namespace MusicApp.Models
{
    public class Album
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int ArtistId { get; set; }
        public int Year { get; set; }
        public string CoverImageUrl { get; set; }
        public string Genre { get; set; }
    }
}

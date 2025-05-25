using System.ComponentModel.DataAnnotations;

namespace MusicApp.ViewModels
{
    public class SongCreateViewModel
    {
        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;
        
        public int? ArtistId { get; set; }
        
        public int? AlbumId { get; set; }
        
        public TimeSpan Duration { get; set; }
        
        [Range(1, int.MaxValue)]
        public int? TrackNumber { get; set; }
        
        [StringLength(50)]
        public string? Genre { get; set; }
        
        public DateTime? ReleaseDate { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace MusicApp.ViewModels
{
    public class AlbumCreateViewModel
    {
        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public int ArtistId { get; set; }
        
        public DateTime? ReleaseDate { get; set; }
    }
}

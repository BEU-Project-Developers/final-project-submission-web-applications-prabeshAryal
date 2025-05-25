using System.ComponentModel.DataAnnotations;

namespace MusicApp.ViewModels
{
    public class ArtistCreateViewModel
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        public string? Bio { get; set; }
        
        [StringLength(50)]
        public string? Country { get; set; }
        
        [StringLength(50)]
        public string? Genre { get; set; }
        
        public DateTime? FormedDate { get; set; }
        
        [Range(0, int.MaxValue)]
        public int? MonthlyListeners { get; set; }
    }
}

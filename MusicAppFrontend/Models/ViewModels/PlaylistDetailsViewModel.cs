using System.ComponentModel.DataAnnotations;

namespace MusicApp.Models.ViewModels
{
    public class PlaylistDetailsViewModel
    {
        public int PlaylistId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<SongViewModel> Songs { get; set; } = new List<SongViewModel>();
        public bool IsOwner { get; set; }
        public string? CoverImageUrl { get; set; }
    }

    public class SongViewModel
    {
        public int SongId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ArtistName { get; set; } = string.Empty;
        public string AlbumName { get; set; } = string.Empty;
        public int Duration { get; set; }
        public string? CoverImageUrl { get; set; }
        public string? SongFileUrl { get; set; }
    }
}

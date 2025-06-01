using System.Collections.Generic;

namespace MusicApp.ViewModels
{
    public class HomeViewModel
    {
        public ArtistViewModel? FeaturedArtist { get; set; }
        public List<AlbumViewModel> LatestAlbums { get; set; } = new List<AlbumViewModel>();
        public List<PlaylistViewModel> PopularPlaylists { get; set; } = new List<PlaylistViewModel>();
        public List<SongViewModel> RecentSongs { get; set; } = new List<SongViewModel>();
    }

    public class ArtistViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public int MonthlyListeners { get; set; }
    }    public class AlbumViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ArtistName { get; set; } = string.Empty;
        public string CoverImageUrl { get; set; } = string.Empty;
        public int? Year { get; set; }
    }

    public class PlaylistViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? CoverImageUrl { get; set; }
        public int SongCount { get; set; }
    }    public class SongViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ArtistName { get; set; } = string.Empty;
        public string CoverImageUrl { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; } // Changed from string to TimeSpan
        public string? AudioUrl { get; set; }
    }
}
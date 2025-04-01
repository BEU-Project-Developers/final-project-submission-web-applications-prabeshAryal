// MusicApp.Models/ProfileViewModel.cs
using System.Collections.Generic;

namespace MusicApp.Models
{
    public class ProfileViewModel
    {
        public string ProfileImageUrl { get; set; }
        public string Username { get; set; }
        public string MembershipStatus { get; set; }
        public int PlaylistsCount { get; set; }
        public int FollowersCount { get; set; }
        public int FollowingCount { get; set; }

        public string TotalListeningTime { get; set; }
        public string TopGenre { get; set; }
        public string FavoriteArtist { get; set; }

        public List<RecentlyPlayedTrackViewModel> RecentlyPlayedTracks { get; set; }
        public List<TopArtistViewModel> TopArtists { get; set; }
        public List<ActivityFeedItemViewModel> ActivityFeedItems { get; set; }
    }

    public class RecentlyPlayedTrackViewModel
    {
        public string SongTitle { get; set; }
        public string ArtistName { get; set; }
        public string AlbumName { get; set; }
        public string CoverImageUrl { get; set; }
        public string Duration { get; set; }
    }

    public class TopArtistViewModel
    {
        public string ArtistName { get; set; }
        public string ArtistImageUrl { get; set; }
        public int PlayCount { get; set; }
    }

    public class ActivityFeedItemViewModel
    {
        public string Description { get; set; }
        public string TimeAgo { get; set; }
        public string IconClass
        {
            get; set; // Bootstrap Icon class (e.g., "bi bi-music-note")
        }
        public string BadgeColorClass
        {
            get; set; // Bootstrap badge color class (e.g., "bg-primary")
        }
    }

}
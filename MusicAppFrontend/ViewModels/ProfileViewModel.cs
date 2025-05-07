using System;
using System.Collections.Generic;
using MusicApp.Models;

namespace MusicApp.ViewModels
{
    public class ProfileViewModel
    {
        public User User { get; set; }
        
        // Add null checks to all properties that reference User
        public string Username => User?.Username ?? "Guest User";
        public string Email => User?.Email ?? "";
        public string FirstName => User?.FirstName ?? "";
        public string LastName => User?.LastName ?? "";
        public string FullName => $"{FirstName} {LastName}".Trim();
        public string ProfileImageUrl => User?.ProfileImageUrl ?? "/images/default-profile.png";
        public string MembershipStatus => "Premium Member"; // This could be dynamic based on user's subscription
        public int PlaylistsCount => User?.Playlists?.Count ?? 0;
        public int FollowersCount => User?.Followers?.Count ?? 0;
        public int FollowingCount => User?.Following?.Count ?? 0;
        
        // Default values for other properties
        public string TotalListeningTime { get; set; } = "0 hours";
        public string TopGenre { get; set; } = "None";
        public string FavoriteArtist { get; set; } = "None";
        
        public List<RecentlyPlayedTrack> RecentlyPlayedTracks { get; set; } = new List<RecentlyPlayedTrack>();
        public List<TopArtist> TopArtists { get; set; } = new List<TopArtist>();
        public List<ActivityFeedItem> ActivityFeedItems { get; set; } = new List<ActivityFeedItem>();
        
        // Add methods to populate the model from API data
        public void AddSampleData()
        {
            // Only add sample data if we have no real data
            if (RecentlyPlayedTracks.Count == 0)
            {
                RecentlyPlayedTracks.Add(new RecentlyPlayedTrack 
                { 
                    Id = 1, 
                    SongTitle = "Sample Song", 
                    ArtistName = "Sample Artist", 
                    AlbumName = "Sample Album", 
                    Duration = "3:45", 
                    CoverImageUrl = "/images/sample-cover.png" 
                });
            }
            
            if (TopArtists.Count == 0)
            {
                TopArtists.Add(new TopArtist 
                { 
                    Id = 1, 
                    ArtistName = "Sample Artist", 
                    PlayCount = 42, 
                    ArtistImageUrl = "/images/sample-artist.png" 
                });
            }
            
            if (ActivityFeedItems.Count == 0)
            {
                ActivityFeedItems.Add(new ActivityFeedItem 
                { 
                    Id = 1, 
                    Type = "playlist_created", 
                    Description = "You created a new playlist", 
                    Timestamp = DateTime.Now.AddDays(-1) 
                });
            }
        }
        
        public class RecentlyPlayedTrack
        {
            public int Id { get; set; }
            public string SongTitle { get; set; }
            public string ArtistName { get; set; }
            public string AlbumName { get; set; }
            public string Duration { get; set; }
            public string CoverImageUrl { get; set; }
        }
        
        public class TopArtist
        {
            public int Id { get; set; }
            public string ArtistName { get; set; }
            public int PlayCount { get; set; }
            public string ArtistImageUrl { get; set; }
        }
        
        public class ActivityFeedItem
        {
            public int Id { get; set; }
            public string Type { get; set; }
            public string Description { get; set; }
            public DateTime Timestamp { get; set; }
            public string TimeAgo => GetTimeAgo(Timestamp);
            public string BadgeColorClass => GetBadgeColorClass(Type);
            public string IconClass => GetIconClass(Type);

            private static string GetTimeAgo(DateTime timestamp)
            {
                var timeSpan = DateTime.UtcNow - timestamp;
                if (timeSpan.TotalDays > 365)
                    return $"{(int)(timeSpan.TotalDays / 365)} years ago";
                if (timeSpan.TotalDays > 30)
                    return $"{(int)(timeSpan.TotalDays / 30)} months ago";
                if (timeSpan.TotalDays > 1)
                    return $"{(int)timeSpan.TotalDays} days ago";
                if (timeSpan.TotalHours > 1)
                    return $"{(int)timeSpan.TotalHours} hours ago";
                if (timeSpan.TotalMinutes > 1)
                    return $"{(int)timeSpan.TotalMinutes} minutes ago";
                return "just now";
            }

            private static string GetBadgeColorClass(string type)
            {
                return type switch
                {
                    "playlist_created" => "bg-primary",
                    "song_liked" => "bg-danger",
                    "artist_followed" => "bg-success",
                    "album_added" => "bg-info",
                    _ => "bg-secondary"
                };
            }

            private static string GetIconClass(string type)
            {
                return type switch
                {
                    "playlist_created" => "bi bi-music-note-list",
                    "song_liked" => "bi bi-heart-fill",
                    "artist_followed" => "bi bi-person-plus-fill",
                    "album_added" => "bi bi-disc-fill",
                    _ => "bi bi-activity"
                };
            }
        }
    }
} 
using Microsoft.AspNetCore.Http; // Required for IFormFile
using System.ComponentModel.DataAnnotations;
using MusicApp.Models; // Required for User model
using System;
using System.Collections.Generic; 
using System.Linq; // Required for List<T> and Any()

namespace MusicApp.ViewModels
{
    public class ProfileViewModel
    {
        // Properties for form binding
        [Required(ErrorMessage = "Full Name is required.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Username is required.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters.")]
        public string Username { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Bio cannot exceed 1000 characters.")]
        public string? Bio { get; set; }

        public IFormFile? ProfileImage { get; set; } // For new image upload, matches form input name="ProfileImage"

        // Properties for display (populated in GET action, not directly part of the edit form submission values)
        public string Email { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty; // Holds the URL for the <img> tag

        // To hold the original user data for initialization and other display purposes from dashboard/profile view
        public User? User { get; set; }

        // Parameterless constructor for model binding
        public ProfileViewModel() { }

        // Method to initialize view model from User object
        public void InitializeFromUser(User user, string backendBaseUrl = "http://localhost:5117") // Allow backend URL to be configurable
        {
            this.User = user; // Store the original user object if needed for other properties

            this.FullName = $"{user.FirstName} {user.LastName}".Trim();
            this.Username = user.Username ?? string.Empty;
            this.Email = user.Email ?? string.Empty;
            this.Bio = user.Bio;

            if (string.IsNullOrEmpty(user.ProfileImageUrl))
            {
                this.ProfileImageUrl = "/assets/default-profile.png"; // Default image
            }
            else if (user.ProfileImageUrl.StartsWith("/uploads")) // Relative path from backend
            {
                this.ProfileImageUrl = $"{backendBaseUrl.TrimEnd('/')}{user.ProfileImageUrl}";
            }
            else
            {
                this.ProfileImageUrl = user.ProfileImageUrl; // Absolute URL or other path
            }
        }

        // --- Properties for dashboard display, not directly for the edit form ---
        public string MembershipStatus => User?.UserRoles?.Any(ur => ur.Role?.Name == "Admin") ?? false ? "Administrator" : "Member";
        public int PlaylistsCount => User?.Playlists?.Count ?? 0;
        public int FollowersCount => User?.Followers?.Count ?? 0;
        public int FollowingCount => User?.Following?.Count ?? 0;
        
        // These might be populated from API calls in a real dashboard scenario
        public string TotalListeningTime { get; set; } = "0 hours";
        public string TopGenre { get; set; } = "Rock";
        public string FavoriteArtist { get; set; } = "Various Artists";
        
        public List<RecentlyPlayedTrack> RecentlyPlayedTracks { get; set; } = new List<RecentlyPlayedTrack>();
        public List<TopArtist> TopArtists { get; set; } = new List<TopArtist>();
        public List<ActivityFeedItem> ActivityFeedItems { get; set; } = new List<ActivityFeedItem>();
        
        public void AddSampleData()
        {
            // Sample data for dashboard development or when real data isn't available
            if (RecentlyPlayedTracks.Count == 0)
            {
                // Note property names now match Dashboard.cshtml usage: SongTitle, ArtistName, Duration, AlbumCoverUrl
                RecentlyPlayedTracks.Add(new RecentlyPlayedTrack { 
                    SongTitle = "Bohemian Rhapsody", 
                    ArtistName = "Queen", 
                    AlbumCoverUrl = "/assets/covers/default-album.png", 
                    PlayedAt = DateTime.Now.AddMinutes(-5),
                    Duration = "5:55"
                });
                RecentlyPlayedTracks.Add(new RecentlyPlayedTrack { 
                    SongTitle = "Shape of You", 
                    ArtistName = "Ed Sheeran", 
                    AlbumCoverUrl = "/assets/covers/default-album.png", 
                    PlayedAt = DateTime.Now.AddHours(-1),
                    Duration = "3:53" 
                });
            }
            
            if (TopArtists.Count == 0)
            {
                // Note property names now match Dashboard.cshtml usage: ArtistName, ArtistImageUrl, PlayCount
                TopArtists.Add(new TopArtist { 
                    ArtistName = "Queen", 
                    ArtistImageUrl = "/assets/artists/default-artist.png", 
                    PlayCount = 42,
                    MonthlyListeners = "10M" 
                });
                TopArtists.Add(new TopArtist { 
                    ArtistName = "Ed Sheeran", 
                    ArtistImageUrl = "/assets/artists/default-artist.png", 
                    PlayCount = 38,
                    MonthlyListeners = "15M" 
                });
            }
            
            if (ActivityFeedItems.Count == 0)
            {
                // Note property names now match Dashboard.cshtml usage: Description, TimeAgo, IconClass
                ActivityFeedItems.Add(new ActivityFeedItem { 
                    Description = "Liked the song 'Stairway to Heaven'", 
                    TimeAgo = "2 hours ago", 
                    IconClass = "bi-heart-fill text-danger",
                    Timestamp = DateTime.Now.AddHours(-2)
                });
                ActivityFeedItems.Add(new ActivityFeedItem { 
                    Description = "Added 'Bohemian Rhapsody' to 'My Favorites' playlist", 
                    TimeAgo = "Yesterday", 
                    IconClass = "bi-plus-circle-fill text-success",
                    Timestamp = DateTime.Now.AddDays(-1)
                });
            }
        }
    }

    // Helper classes for dashboard items with property names matching those used in Dashboard.cshtml
    public class RecentlyPlayedTrack
    {
        public string SongTitle { get; set; } = string.Empty;
        public string ArtistName { get; set; } = string.Empty;
        public string AlbumCoverUrl { get; set; } = string.Empty; // This property is used in Dashboard.cshtml
        public string Duration { get; set; } = string.Empty; // This property is used in Dashboard.cshtml
        public DateTime PlayedAt { get; set; } = DateTime.Now;
        // For compatibility with previously used property names
        public string Title => SongTitle;
        public string Artist => ArtistName;
    }

    public class TopArtist
    {
        public string ArtistName { get; set; } = string.Empty; // This property is used in Dashboard.cshtml
        public string ArtistImageUrl { get; set; } = string.Empty; // This property is used in Dashboard.cshtml
        public int PlayCount { get; set; } = 0; // This property is used in Dashboard.cshtml
        public string MonthlyListeners { get; set; } = string.Empty;
        // For compatibility with previously used property names
        public string Name => ArtistName;
        public string ImageUrl => ArtistImageUrl;
    }

    public class ActivityFeedItem
    {
        public string Description { get; set; } = string.Empty;
        public string TimeAgo { get; set; } = string.Empty; // This property is used in Dashboard.cshtml
        public string IconClass { get; set; } = string.Empty; // e.g., "bi-heart", "bi-plus-circle"
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
// MusicApp.Controllers/ProfileController.cs
using Microsoft.AspNetCore.Mvc;
using MusicApp.Models;
using System;
using System.Collections.Generic;

namespace MusicApp.Controllers
{
    public class ProfileController : Controller
    {
        private string GetRandomImageUrl(int width, int height, int idStartRange)
        {
            Random random = new Random();
            // Corrected range: 1 to 1084 inclusive
            int randomId = random.Next(1, 1085); // Using 1085 to make 1084 inclusive
            return $"https://picsum.photos/id/{randomId}/{width}/{height}";
        }

        public IActionResult Index()
        {
            var viewModel = new ProfileViewModel
            {
                ProfileImageUrl = GetRandomImageUrl(120, 120, 8001), // idStartRange is still used for variety, but the random ID is limited to 1-1084
                Username = "Prabesh Aryal",
                MembershipStatus = "Super Administrator",
                PlaylistsCount = new Random().Next(20, 60),
                FollowersCount = new Random().Next(80, 200),
                FollowingCount = new Random().Next(50, 150),

                TotalListeningTime = $"{new Random().Next(100, 200)} hours",
                TopGenre = new string[] { "Rock", "Pop", "Electronic", "Hip Hop", "Classical" }[new Random().Next(0, 5)],
                FavoriteArtist = new string[] { "The Beatles", "Queen", "Radiohead", "Coldplay", "Nirvana" }[new Random().Next(0, 5)],

                RecentlyPlayedTracks = GenerateRecentlyPlayedTracks(),
                TopArtists = GenerateTopArtists(),
                ActivityFeedItems = GenerateActivityFeedItems()
            };

            return View(viewModel);
        }

        private List<RecentlyPlayedTrackViewModel> GenerateRecentlyPlayedTracks()
        {
            var tracks = new List<RecentlyPlayedTrackViewModel>();
            for (int i = 1; i <= 5; i++)
            {
                tracks.Add(new RecentlyPlayedTrackViewModel
                {
                    SongTitle = $"Song Title {i}",
                    ArtistName = $"Artist Name {i}",
                    AlbumName = $"Album Name {i}",
                    CoverImageUrl = GetRandomImageUrl(50, 50, 9000 + i * 100), // idStartRange is still used for variety, but the random ID is limited to 1-1084
                    Duration = $"{new Random().Next(3, 5)}:{new Random().Next(10, 60).ToString("D2")}"
                });
            }
            return tracks;
        }

        private List<TopArtistViewModel> GenerateTopArtists()
        {
            var topArtists = new List<TopArtistViewModel>();
            string[] artistNames = new string[] { "Artist A", "Artist B", "Artist C", "Artist D" };
            for (int i = 0; i < 4; i++)
            {
                topArtists.Add(new TopArtistViewModel
                {
                    ArtistName = artistNames[i],
                    ArtistImageUrl = GetRandomImageUrl(80, 80, 10000 + i * 100), // idStartRange is still used for variety, but the random ID is limited to 1-1084
                    PlayCount = new Random().Next(30, 80)
                });
            }
            return topArtists;
        }

        private List<ActivityFeedItemViewModel> GenerateActivityFeedItems()
        {
            var activities = new List<ActivityFeedItemViewModel>();
            activities.Add(new ActivityFeedItemViewModel
            {
                Description = "You added 5 songs to <strong>Workout Playlist</strong>",
                TimeAgo = "30 minutes ago",
                IconClass = "bi bi-music-note",
                BadgeColorClass = "bg-primary"
            });
            activities.Add(new ActivityFeedItemViewModel
            {
                Description = "You followed <strong>MusicLover123</strong>",
                TimeAgo = "1 hour ago",
                IconClass = "bi bi-people",
                BadgeColorClass = "bg-success"
            });
            activities.Add(new ActivityFeedItemViewModel
            {
                Description = "You liked <strong>Chill Beats</strong> by <strong>Relax Sounds</strong>",
                TimeAgo = "2 hours ago",
                IconClass = "bi bi-heart",
                BadgeColorClass = "bg-info"
            });
            return activities;
        }
    }
}
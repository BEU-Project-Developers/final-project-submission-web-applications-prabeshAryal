// MusicApp.Controllers/ProfileController.cs
using Microsoft.AspNetCore.Mvc;
using MusicApp.Models;
using MusicApp.ViewModels;
using System;
using System.Collections.Generic;

namespace MusicApp.Controllers
{
    public class ProfileController : Controller
    {
        private static string GetRandomImageUrl(int width, int height)
        {
            Random random = new Random();
            // Corrected range: 1 to 1084 inclusive
            int randomId = random.Next(1, 1085); // Using 1085 to make 1084 inclusive
            return $"https://picsum.photos/id/{randomId}/{width}/{height}";
        }

        public IActionResult Index()
        {
            // Create a sample user
            var user = new User
            {
                Username = "Prabesh Aryal",
                ProfileImageUrl = "~/assets/default-profile.png",
                Playlists = new List<Playlist>(),
                Followers = new List<UserFollower>(),
                Following = new List<UserFollower>()
            };

            // Add sample data to user
            for (int i = 0; i < 15; i++)
            {
                user.Playlists.Add(new Playlist());
            }
            for (int i = 0; i < 234; i++)
            {
                user.Followers.Add(new UserFollower());
            }
            for (int i = 0; i < 123; i++)
            {
                user.Following.Add(new UserFollower());
            }

            var model = new ProfileViewModel
            {
                User = user,
                TotalListeningTime = "1,234 hours",
                TopGenre = "Rock",
                FavoriteArtist = "Artist Name",
                RecentlyPlayedTracks = GenerateRecentlyPlayedTracks(),
                TopArtists = GenerateTopArtists(),
                ActivityFeedItems = GenerateActivityFeedItems()
            };

            return View(model);
        }

        private List<ProfileViewModel.RecentlyPlayedTrack> GenerateRecentlyPlayedTracks()
        {
            var tracks = new List<ProfileViewModel.RecentlyPlayedTrack>();

            // Add sample tracks
            tracks.Add(new ProfileViewModel.RecentlyPlayedTrack
            {
                Id = 1,
                SongTitle = "Song Title 1",
                ArtistName = "Artist 1",
                AlbumName = "Album 1",
                CoverImageUrl = "~/assets/album-cover.jpg",
                Duration = "3:45"
            });

            return tracks;
        }

        private List<ProfileViewModel.TopArtist> GenerateTopArtists()
        {
            var topArtists = new List<ProfileViewModel.TopArtist>();

            // Add sample artists
            topArtists.Add(new ProfileViewModel.TopArtist
            {
                Id = 1,
                ArtistName = "Artist 1",
                ArtistImageUrl = "~/assets/artist-1.jpg",
                PlayCount = 1234
            });

            return topArtists;
        }

        private List<ProfileViewModel.ActivityFeedItem> GenerateActivityFeedItems()
        {
            var activities = new List<ProfileViewModel.ActivityFeedItem>();
            activities.Add(new ProfileViewModel.ActivityFeedItem
            {
                Id = 1,
                Description = "Listened to <strong>Song Title</strong>",
                Type = "song_played",
                Timestamp = DateTime.UtcNow.AddMinutes(-3)
            });

            activities.Add(new ProfileViewModel.ActivityFeedItem
            {
                Id = 2,
                Description = "Added <strong>Album Name</strong> to favorites",
                Type = "album_favorited",
                Timestamp = DateTime.UtcNow.AddHours(-1)
            });

            activities.Add(new ProfileViewModel.ActivityFeedItem
            {
                Id = 3,
                Description = "Created new playlist <strong>My Playlist</strong>",
                Type = "playlist_created",
                Timestamp = DateTime.UtcNow.AddHours(-2)
            });

            return activities;
        }
    }
}
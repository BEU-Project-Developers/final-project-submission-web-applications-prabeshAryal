using Microsoft.AspNetCore.Mvc;
using MusicApp.Models;
using System.Collections.Generic;

namespace MusicApp.Controllers
{
    public class PlaylistsController : Controller
    {
        public IActionResult Index()
        {
            var playlists = new List<Playlist>
            {
                new Playlist
                {
                    Id = 1,
                    Name = "Workout Mix",
                    Description = "High-energy tracks for your workout",
                    CoverImageUrl = "~/assets/playlist-1.jpg",
                    IsPublic = true,
                    CreatedAt = System.DateTime.Now.AddDays(-5),
                    Songs = new List<Song>() // Initialize empty collection
                },
                new Playlist
                {
                    Id = 2,
                    Name = "Chill Vibes",
                    Description = "Relaxing tunes for your downtime",
                    CoverImageUrl = "~/assets/playlist-2.jpg",
                    IsPublic = true,
                    CreatedAt = System.DateTime.Now.AddDays(-10),
                    Songs = new List<Song>() // Initialize empty collection
                },
                new Playlist
                {
                    Id = 3,
                    Name = "Road Trip",
                    Description = "Perfect for your next adventure",
                    CoverImageUrl = "~/assets/playlist-3.jpg",
                    IsPublic = false,
                    CreatedAt = System.DateTime.Now.AddDays(-15),
                    Songs = new List<Song>() // Initialize empty collection
                }
            };

            return View(playlists);
        }

        public IActionResult Create()
        {
            return View();
        }
    }
}

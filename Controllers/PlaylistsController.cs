using Microsoft.AspNetCore.Mvc;
using MusicApp.Models;

namespace MusicApp.Controllers
{
    public class PlaylistsController : Controller
    {
        public IActionResult Index()
        {
            // Create sample playlists for testing
            var playlists = new List<Playlist>
        {
            new Playlist
            {
                Id = 1,
                Name = "Workout Mix",
                Description = "High energy songs for your workout session",
                CoverImageUrl = "https://picsum.photos/id/399/400/400",
                IsPublic = true,
                SongIds = new List<int> { 1, 2, 3, 4, 5 }
            },
            new Playlist
            {
                Id = 2,
                Name = "Chill Vibes",
                Description = "Relaxing music for a calm evening",
                CoverImageUrl = "https://picsum.photos/id/103/400/400",
                IsPublic = true,
                SongIds = new List<int> { 6, 7, 8, 9 }
            },
            new Playlist
            {
                Id = 3,
                Name = "Study Focus",
                Description = "Concentration music for productive study sessions",
                CoverImageUrl = "https://picsum.photos/id/4/400/400",
                IsPublic = false,
                SongIds = new List<int> { 10, 11, 12 }
            }
            // Add more as needed
        };

            return View(playlists);
        }

        public IActionResult Create()
        {
            return View();
        }
    }
}

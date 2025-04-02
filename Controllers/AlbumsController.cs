using Microsoft.AspNetCore.Mvc;
using MusicApp.Models;

namespace MusicApp.Controllers
{
    public class AlbumsController : Controller
    {
    
            public IActionResult Index()
            {
            // For testing purposes, create some sample albums
            var albums = new List<Album>
{
    new Album
    {
        Id = 1,
        Title = "Echoes of the Valley",
        ArtistId = 1,
        Year = 2021,
        CoverImageUrl = $"https://picsum.photos/id/{new Random().Next(1, 1084)}/400/400", // Random picsum image
        Genre = "Folk"
    },
    new Album
    {
        Id = 2,
        Title = "Neon City Nights",
        ArtistId = 2,
        Year = 2023,
        CoverImageUrl = $"https://picsum.photos/id/{new Random().Next(1, 1084)}/400/400", // Another random picsum image
        Genre = "Synthwave"
    },
    new Album
    {
        Id = 3,
        Title = "Midnight Bloom",
        ArtistId = 3,
        Year = 2024,
        CoverImageUrl = $"https://picsum.photos/id/{new Random().Next(1, 1084)}/400/400", // Yet another random picsum image
        Genre = "Indie Pop"
    },
    new Album
    {
        Id = 4,
        Title = "Rhythmic Pulse",
        ArtistId = 4,
        Year = 2022,
        CoverImageUrl = $"https://picsum.photos/id/{new Random().Next(1, 1084)}/400/400", // And one more!
        Genre = "Electronic"
    }
    // We can easily add more sample albums following this pattern!
};

            return View(albums);
            }
   

        public IActionResult Details(int id)
        {
            return View();
        }
    }
}

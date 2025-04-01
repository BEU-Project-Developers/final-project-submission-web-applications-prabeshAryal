// MusicApp.Controllers/ArtistsController.cs
using Microsoft.AspNetCore.Mvc;
using MusicApp.Models; // Make sure to include your Models namespace
using System;
using System.Collections.Generic;
using System.Linq;

namespace MusicApp.Controllers
{
    public class ArtistsController : Controller
    {
        private readonly string[] artistNames = new string[]
        {
            "Aurora Rivers", "Ethan Blackwood", "Seraphina Stone", "Jasper Hayes",
            "Willow Creek", "Caspian Vance", "Indigo Skye", "Zephyr Wilde",
            "Luna Bloom", "Orion Frost"
        };

        private readonly string[] genres = new string[]
        {
            "Pop", "Rock", "Hip Hop", "Jazz", "Electronic", "Classical", "Country", "R&B"
        };

        private string GetRandomImageUrl(int width, int height)
        {
            Random random = new Random();
            int randomId = random.Next(1, 1000);
            return $"https://picsum.photos/id/{randomId}/{width}/{height}";
        }

        public IActionResult Index()
        {
            var artists = new List<Artist>();
            Random random = new Random();

            for (int i = 1; i <= 6; i++) // Generate 6 artists for the Index view
            {
                artists.Add(new Artist
                {
                    Id = i, // Or generate a unique ID if needed
                    Name = artistNames[i - 1 < artistNames.Length ? i - 1 : 0],
                    ImageUrl = GetRandomImageUrl(500, 300),
                    Genre = genres[random.Next(0, genres.Length)],
                    AlbumCount = random.Next(3, 18)
                });
            }

            return View(artists); // Pass the list of artists to the view
        }

        public IActionResult Details(int id)
        {
            return View();
        }
    }
}
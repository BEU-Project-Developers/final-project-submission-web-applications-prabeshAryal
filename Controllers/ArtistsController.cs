using Microsoft.AspNetCore.Mvc;
using MusicApp.Models; // Make sure to include your Models namespace
using System;
using System.Collections.Generic;
using System.Linq;

namespace MusicApp.Controllers
{
    public class ArtistsController : Controller
    {
        private readonly Random random = new Random();

        // Sample data (Consider moving to a service layer in a real app)
        private readonly string[] artistNames = new string[]
        {
            "Aurora Rivers", "Ethan Blackwood", "Seraphina Stone", "Jasper Hayes",
            "Willow Creek", "Caspian Vance", "Indigo Skye", "Zephyr Wilde",
            "Luna Bloom", "Orion Frost", "Crimson Echo", "Silver Moon" // Added more names
        };

        private readonly string[] genres = new string[]
        {
            "Synthwave", "Indie Pop", "Alternative Rock", "Ambient", "Electro Swing",
            "Chillhop", "Dream Pop", "Future Bass", "Lo-fi Hip Hop", "Deep House" // More specific genres
        };

        // Helper to get a random square image URL
        private string GetRandomImageUrl(int size) // Request size (width & height)
        {
            int randomId = random.Next(1, 500);
            return $"https://picsum.photos/id/{randomId}/{size}/{size}"; // Request square image
        }

        // GET: /Artists/
        public IActionResult Index()
        {
            var artists = new List<Artist>
            {
                new Artist
                {
                    Id = 1,
                    Name = "Artist 1",
                    Bio = "A talented musician known for their unique style",
                    ImageUrl = "~/assets/artist-1.jpg",
                    Genre = "Rock",
                    FormedDate = DateTime.Now.AddYears(-10),
                    Country = "USA",
                    MonthlyListeners = random.Next(1000, 10000),
                    Albums = new List<Album>(),
                    Songs = new List<Song>(),
                    Followers = new List<User>()
                },
                new Artist
                {
                    Id = 2,
                    Name = "Artist 2",
                    Bio = "An innovative artist pushing boundaries",
                    ImageUrl = "~/assets/artist-2.jpg",
                    Genre = "Pop",
                    FormedDate = DateTime.Now.AddYears(-5),
                    Country = "UK",
                    MonthlyListeners = random.Next(1000, 10000),
                    Albums = new List<Album>(),
                    Songs = new List<Song>(),
                    Followers = new List<User>()
                }
            };

            return View(artists);
        }

        // GET: /Artists/Details/5
        public IActionResult Details(int id)
        {
            // TODO: Get actual artist data from database
            var artist = new Artist
            {
                Id = id,
                Name = "Artist Name",
                Bio = "A detailed biography of the artist...",
                ImageUrl = "~/assets/artist-details.jpg",
                Genre = "Rock",
                FormedDate = DateTime.Now.AddYears(-10),
                Country = "USA",
                MonthlyListeners = random.Next(1000, 10000),
                Albums = new List<Album>(),
                Songs = new List<Song>(),
                Followers = new List<User>()
            };

            return View(artist);
        }
    }
}

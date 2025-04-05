using Microsoft.AspNetCore.Mvc;
using MusicApp.Models; // Make sure to include your Models namespace
using System;
using System.Collections.Generic;
using System.Linq;

namespace MusicApp.Controllers
{
    public class ArtistsController : Controller
    {
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
            Random random = new Random();
            int randomId = random.Next(1, 500);
            return $"https://picsum.photos/id/{randomId}/{size}/{size}"; // Request square image
        }

        // GET: /Artists/
        public IActionResult Index()
        {
            var artists = new List<Artist>();
            Random random = new Random();
            int totalArtists = 12;

            for (int i = 1; i <= totalArtists; i++)
            {
                string name = artistNames[(i - 1) % artistNames.Length];
                string genre = genres[random.Next(0, genres.Length)];

                artists.Add(new Artist
                {
                    Id = i,
                    Name = name,
                    ImageUrl = GetRandomImageUrl(300),
                    Genre = genre,
                    AlbumCount = random.Next(1, 9)
                });
            }
            return View(artists);
        }

        // GET: /Artists/Details/5
        public IActionResult Details(int id)
        {
            // --- Mock Details Fetch ---
            // In a real app, fetch the specific artist by ID from your data source.
            // If the artist might not be found, you'd check for null *before* creating the object.
            // Example: var artistData = _database.Artists.Find(id); if (artistData == null) return NotFound();

            Random random = new Random();

            // Ensure the ID maps reasonably to the available names for mock data
            if (id <= 0 || id > artistNames.Length) // Basic check for valid mock ID range
            {
                // Handle invalid ID - redirect to Index or show a specific error view
                // return NotFound(); // Or RedirectToAction("Index");
                // For now, let's create a default artist to avoid errors if ID is out of bounds for mock names
                id = 1; // Default to first artist if ID is invalid for mock data
            }

            string name = artistNames[(id - 1) % artistNames.Length]; // Get name based on ID (cyclical)
            string genre = genres[random.Next(0, genres.Length)];

            var artist = new Artist
            {
                Id = id,
                Name = name,
                ImageUrl = GetRandomImageUrl(400), // Larger image for details page
                Genre = genre,
                AlbumCount = random.Next(1, 9)
                // Add more properties like Bio, list of Albums, etc. if needed
            };

            // --- FIX: Pass the 'artist' object (the Model) to the View ---
            return View(artist);
        }
    }
}

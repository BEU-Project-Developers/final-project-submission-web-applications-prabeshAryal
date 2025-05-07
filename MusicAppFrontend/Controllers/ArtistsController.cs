using Microsoft.AspNetCore.Mvc;
using MusicApp.Models;
using MusicApp.Models.DTOs;
using MusicApp.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MusicApp.Controllers
{
    public class ArtistsController : Controller
    {
        private readonly ApiService _apiService;

        public ArtistsController(ApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var response = await _apiService.GetAsync<PagedResponse<ArtistDto>>("api/Artists");
                
                // Return the data from the API response
                return View(response?.Data ?? new List<ArtistDto>());
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error retrieving artists: {ex.Message}");
                
                // Return an empty list with error message
                ViewBag.ErrorMessage = "Unable to load artists from the server. Please try again later.";
                return View(new List<ArtistDto>());
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var artist = await _apiService.GetAsync<ArtistDto>($"api/Artists/{id}");
                
                if (artist == null)
                {
                    return NotFound();
                }
                
                return View(artist);
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error retrieving artist details: {ex.Message}");
                
                // Redirect to index with error message
                TempData["ErrorMessage"] = "Unable to load artist details. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}

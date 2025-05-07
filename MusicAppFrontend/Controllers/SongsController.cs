using Microsoft.AspNetCore.Mvc;
using MusicApp.Models;
using MusicApp.Models.DTOs;
using MusicApp.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MusicApp.Controllers
{
    public class SongsController : Controller
    {
        private readonly ApiService _apiService;

        public SongsController(ApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var response = await _apiService.GetAsync<PagedResponse<SongDto>>("api/Songs");
                
                // Return the data from the API response
                return View(response?.Data ?? new List<SongDto>());
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error retrieving songs: {ex.Message}");
                
                // Return an empty list with error message
                ViewBag.ErrorMessage = "Unable to load songs from the server. Please try again later.";
                return View(new List<SongDto>());
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var song = await _apiService.GetAsync<SongDto>($"api/Songs/{id}");
                
                if (song == null)
                {
                    return NotFound();
                }
                
                return View(song);
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error retrieving song details: {ex.Message}");
                
                // Redirect to index with error message
                TempData["ErrorMessage"] = "Unable to load song details. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
} 
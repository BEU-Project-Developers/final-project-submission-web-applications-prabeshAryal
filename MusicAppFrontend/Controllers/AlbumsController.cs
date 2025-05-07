using Microsoft.AspNetCore.Mvc;
using MusicApp.Models;
using MusicApp.Models.DTOs;
using MusicApp.Services;
using MusicApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MusicApp.Controllers
{
    public class AlbumsController : Controller
    {
        private readonly ApiService _apiService;

        public AlbumsController(ApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var response = await _apiService.GetAsync<PagedResponse<AlbumDto>>("api/Albums");
                
                // Return the data from the API response
                return View(response?.Data ?? new List<AlbumDto>());
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error retrieving albums: {ex.Message}");
                
                // Return an empty list with error message
                ViewBag.ErrorMessage = "Unable to load albums from the server. Please try again later.";
                return View(new List<AlbumDto>());
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var album = await _apiService.GetAsync<AlbumDto>($"api/Albums/{id}");
                
                if (album == null)
                {
                    return NotFound();
                }
                
                return View(album);
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error retrieving album details: {ex.Message}");
                
                // Redirect to index with error message
                TempData["ErrorMessage"] = "Unable to load album details. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}

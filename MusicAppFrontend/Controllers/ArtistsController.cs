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

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Bio,Country,Genre,FormedDate,MonthlyListeners")] ArtistCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            try
            {
                // Map to backend DTO
                var artistCreateDto = new {
                    Name = model.Name,
                    Bio = model.Bio,
                    Country = model.Country,
                    Genre = model.Genre,
                    FormedDate = model.FormedDate,
                    MonthlyListeners = model.MonthlyListeners,
                    IsActive = true
                };
                var result = await _apiService.PostAsync<object>("api/Artists", artistCreateDto);
                if (result != null)
                {
                    TempData["SuccessMessage"] = "Artist added successfully.";
                    return RedirectToAction("Index");
                }
                ViewBag.ErrorMessage = "Failed to add artist.";
            }
            catch (Exception ex)
            {            ViewBag.ErrorMessage = $"Error: {ex.Message}";
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var artist = await _apiService.GetAsync<ArtistDto>($"api/Artists/{id}");
            if (artist == null)
            {
                return NotFound();
            }
            return View(artist);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ArtistDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            try
            {
                var updateDto = new {
                    Name = model.Name,
                    Genre = model.Genre,
                    Bio = model.Bio,
                    Country = model.Country,
                    FormedDate = model.FormedDate,
                    MonthlyListeners = model.MonthlyListeners
                };
                await _apiService.PutAsync<object>($"api/Artists/{id}", updateDto);
                TempData["SuccessMessage"] = "Artist updated successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error updating artist: {ex.Message}";
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var artist = await _apiService.GetAsync<ArtistDto>($"api/Artists/{id}");
            if (artist == null)
            {
                return NotFound();
            }
            return View(artist);
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _apiService.DeleteAsync($"api/Artists/{id}");
                TempData["SuccessMessage"] = "Artist deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting artist: {ex.Message}";
            }
            return RedirectToAction("Index");
        }
    }
}

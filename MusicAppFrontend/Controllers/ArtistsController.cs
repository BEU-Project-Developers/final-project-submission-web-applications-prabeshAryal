using Microsoft.AspNetCore.Mvc;
using MusicApp.Models;
using MusicApp.Models.DTOs;
using MusicApp.Services;
using MusicApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace MusicApp.Controllers
{    public class ArtistsController : Controller
    {
        private readonly ApiService _apiService;
        private readonly ILogger<ArtistsController> _logger;

        public ArtistsController(ApiService apiService, ILogger<ArtistsController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
        {
            try
            {
                var response = await _apiService.GetAsync<PagedResponse<ArtistDto>>($"api/Artists?page={page}&pageSize={pageSize}");
                if (response == null)
                {
                    response = new PagedResponse<ArtistDto>
                    {
                        Data = new List<ArtistDto>(),
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalPages = 1,
                        TotalCount = 0
                    };
                }
                return View(response);            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving artists: {ErrorMessage}", ex.Message);
                ViewBag.ErrorMessage = "Unable to load artists from the server. Please try again later.";
                return View(new PagedResponse<ArtistDto>
                {
                    Data = new List<ArtistDto>(),
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalPages = 1,
                    TotalCount = 0
                });
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
            }            catch (Exception ex)
            {
                // Log the error
                _logger.LogError(ex, "Error retrieving artist details: {ErrorMessage}", ex.Message);

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
                var artistCreateDto = new
                {
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
            {
                ViewBag.ErrorMessage = $"Error: {ex.Message}";
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
            {                var updateDto = new ArtistUpdateDTO
                {
                    Id = id,
                    Name = model.Name,
                    Genre = model.Genre,
                    Bio = model.Bio,
                    Country = model.Country,
                    FormedDate = model.FormedDate,
                    MonthlyListeners = model.MonthlyListeners,
                    ImageUrl = model.ImageUrl,
                    IsActive = model.IsActive
                };
                await _apiService.PutAsync<object>($"api/Artists/{id}", updateDto);
                TempData["SuccessMessage"] = "Artist updated successfully.";
                return RedirectToAction("Index");
            }            catch (Exception ex)
            {
                // Log the detailed error, including status code if it's an HttpRequestException
                var errorMessage = $"Error updating artist: {ex.Message}";
                if (ex is HttpRequestException httpEx && httpEx.StatusCode.HasValue)
                {
                    errorMessage += $" Status Code: {httpEx.StatusCode.Value}";
                }
                _logger.LogError(ex, "Error updating artist: {ErrorMessage}", errorMessage);
                ViewBag.ErrorMessage = errorMessage;
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
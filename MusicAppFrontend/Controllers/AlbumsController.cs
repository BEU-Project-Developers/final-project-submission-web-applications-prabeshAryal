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

        public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
        {
            try
            {
                var response = await _apiService.GetAsync<PagedResponse<AlbumDto>>($"api/Albums?page={page}&pageSize={pageSize}");
                if (response == null)
                {
                    response = new PagedResponse<AlbumDto> {
                        Data = new List<AlbumDto>(),
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalPages = 1,
                        TotalCount = 0
                    };
                }
                return View(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving albums: {ex.Message}");
                ViewBag.ErrorMessage = "Unable to load albums from the server. Please try again later.";
                return View(new PagedResponse<AlbumDto> {
                    Data = new List<AlbumDto>(),
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
        }        [HttpGet]
        public async Task<IActionResult> Create()
        {
            try
            {
                // Load artists for dropdown
                var artists = await _apiService.GetAsync<PagedResponse<ArtistDto>>("api/Artists");
                ViewBag.Artists = artists?.Data ?? new List<ArtistDto>();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Unable to load artists. Please try again later.";
                ViewBag.Artists = new List<ArtistDto>();
            }
            return View();
        }        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,ArtistId,ReleaseDate")] AlbumCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                try
                {
                    // Reload artists for dropdown
                    var artists = await _apiService.GetAsync<PagedResponse<ArtistDto>>("api/Artists");
                    ViewBag.Artists = artists?.Data ?? new List<ArtistDto>();
                }
                catch
                {
                    ViewBag.Artists = new List<ArtistDto>();
                }
                return View(model);
            }
            try
            {
                // Map to backend DTO
                var albumCreateDto = new {
                    Title = model.Title,
                    ArtistId = model.ArtistId,
                    ReleaseDate = model.ReleaseDate
                };
                var result = await _apiService.PostAsync<object>("api/Albums", albumCreateDto);
                if (result != null)
                {
                    TempData["SuccessMessage"] = "Album added successfully.";
                    return RedirectToAction("Index");
                }
                ViewBag.ErrorMessage = "Failed to add album.";
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error: {ex.Message}";
            }
            
            // Reload artists for dropdown on error
            try
            {
                var artists = await _apiService.GetAsync<PagedResponse<ArtistDto>>("api/Artists");
                ViewBag.Artists = artists?.Data ?? new List<ArtistDto>();
            }
            catch
            {
                ViewBag.Artists = new List<ArtistDto>();
            }            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var album = await _apiService.GetAsync<AlbumDto>($"api/Albums/{id}");
            if (album == null)
            {
                return NotFound();
            }
            return View(album);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AlbumDto model)
        {
            Console.WriteLine($"AlbumsController.Edit POST action hit with id: {id} and model.Id: {model.Id}"); // New log
            if (id != model.Id)
            {
                Console.WriteLine("AlbumsController.Edit POST: id mismatch."); // New log
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                Console.WriteLine("AlbumsController.Edit POST: ModelState is invalid."); // New log
                // Log ModelState errors
                foreach (var entry in ModelState.Values)
                {
                    foreach (var error in entry.Errors)
                    {
                        Console.WriteLine($"ModelState Error: {error.ErrorMessage}");
                    }
                }
                return View(model);
            }
            try
            {
                Console.WriteLine("AlbumsController.Edit POST: Attempting to update album."); // New log
                var updateDto = new AlbumUpdateDto
                {
                    Title = model.Title ?? string.Empty,
                    ArtistId = model.ArtistId,
                    Year = model.Year,
                    Description = model.Description ?? string.Empty,
                    Genre = model.Genre ?? string.Empty,
                    ReleaseDate = model.ReleaseDate,
                    TotalTracks = model.TotalTracks,
                    Duration = model.Duration,
                    // CoverImageUrl is not part of AlbumUpdateDTO as it's handled separately
                };
                await _apiService.PutAsync<object>($"api/Albums/{id}", updateDto);
                TempData["SuccessMessage"] = "Album updated successfully.";
                return RedirectToAction("Index");
            }
            catch (HttpRequestException httpEx)
            {
                // Log the full exception details, including status code if available
                Console.WriteLine($"HTTP request error updating album: {httpEx.Message}, Status Code: {httpEx.StatusCode}");
                ViewBag.ErrorMessage = $"Error updating album: {httpEx.Message}. Status Code: {httpEx.StatusCode}";
                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Generic error updating album: {ex.Message}");
                ViewBag.ErrorMessage = $"Error updating album: {ex.Message}";
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var album = await _apiService.GetAsync<AlbumDto>($"api/Albums/{id}");
            if (album == null)
            {
                return NotFound();
            }
            return View(album);
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _apiService.DeleteAsync($"api/Albums/{id}");
                TempData["SuccessMessage"] = "Album deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting album: {ex.Message}";
            }
            return RedirectToAction("Index");
        }
    }
}

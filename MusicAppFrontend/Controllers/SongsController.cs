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
        }        [HttpGet]
        public async Task<IActionResult> Create()
        {
            try
            {
                // Load artists and albums for dropdowns
                var artists = await _apiService.GetAsync<PagedResponse<ArtistDto>>("api/Artists");
                var albums = await _apiService.GetAsync<PagedResponse<AlbumDto>>("api/Albums");
                ViewBag.Artists = artists?.Data ?? new List<ArtistDto>();
                ViewBag.Albums = albums?.Data ?? new List<AlbumDto>();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Unable to load artists and albums. Please try again later.";
                ViewBag.Artists = new List<ArtistDto>();
                ViewBag.Albums = new List<AlbumDto>();
            }
            return View();
        }        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,ArtistId,AlbumId,Duration,TrackNumber,Genre,ReleaseDate")] SongCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                try
                {
                    // Reload dropdowns for validation failure
                    var artists = await _apiService.GetAsync<PagedResponse<ArtistDto>>("api/Artists");
                    var albums = await _apiService.GetAsync<PagedResponse<AlbumDto>>("api/Albums");
                    ViewBag.Artists = artists?.Data ?? new List<ArtistDto>();
                    ViewBag.Albums = albums?.Data ?? new List<AlbumDto>();
                }
                catch
                {
                    ViewBag.Artists = new List<ArtistDto>();
                    ViewBag.Albums = new List<AlbumDto>();
                }
                return View(model);
            }
            try
            {
                // Map to backend DTO
                var songCreateDto = new {
                    Title = model.Title,
                    ArtistId = model.ArtistId,
                    AlbumId = model.AlbumId,
                    Duration = model.Duration,
                    TrackNumber = model.TrackNumber,
                    Genre = model.Genre,
                    ReleaseDate = model.ReleaseDate
                };
                var result = await _apiService.PostAsync<object>("api/Songs", songCreateDto);
                if (result != null)
                {
                    TempData["SuccessMessage"] = "Song added successfully.";
                    return RedirectToAction("Index");
                }
                ViewBag.ErrorMessage = "Failed to add song.";
            }            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error: {ex.Message}";
            }
            
            // Reload dropdowns on error
            try
            {
                var artists = await _apiService.GetAsync<PagedResponse<ArtistDto>>("api/Artists");
                var albums = await _apiService.GetAsync<PagedResponse<AlbumDto>>("api/Albums");
                ViewBag.Artists = artists?.Data ?? new List<ArtistDto>();
                ViewBag.Albums = albums?.Data ?? new List<AlbumDto>();
            }
            catch
            {
                ViewBag.Artists = new List<ArtistDto>();
                ViewBag.Albums = new List<AlbumDto>();
            }            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var song = await _apiService.GetAsync<SongDto>($"api/Songs/{id}");
            if (song == null)
            {
                return NotFound();
            }
            return View(song);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SongDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            try
            {
                var updateDto = new {
                    Title = model.Title,
                    ArtistId = model.ArtistId,
                    AlbumId = model.AlbumId,
                    Duration = model.Duration,
                    TrackNumber = model.TrackNumber,
                    Genre = model.Genre,
                    ReleaseDate = model.ReleaseDate
                };
                await _apiService.PutAsync<object>($"api/Songs/{id}", updateDto);
                TempData["SuccessMessage"] = "Song updated successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error updating song: {ex.Message}";
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var song = await _apiService.GetAsync<SongDto>($"api/Songs/{id}");
            if (song == null)
            {
                return NotFound();
            }
            return View(song);
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _apiService.DeleteAsync($"api/Songs/{id}");
                TempData["SuccessMessage"] = "Song deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting song: {ex.Message}";
            }
            return RedirectToAction("Index");
        }
    }
}
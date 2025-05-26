using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MusicApp.Models;
using MusicApp.Models.DTOs;
using MusicApp.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MusicApp.Controllers
{
    public class PlaylistsController : Controller
    {
        private readonly ApiService _apiService;
        private readonly FileUploadService _fileUploadService;

        public PlaylistsController(ApiService apiService, FileUploadService fileUploadService)
        {
            _apiService = apiService;
            _fileUploadService = fileUploadService;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
        {
            try
            {
                var response = await _apiService.GetAsync<PagedResponse<PlaylistDto>>($"api/Playlists?page={page}&pageSize={pageSize}");
                // Always return a PagedResponse, even if null
                if (response == null)
                {
                    response = new PagedResponse<PlaylistDto> {
                        Data = new List<PlaylistDto>(),
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
                Console.WriteLine($"Error retrieving playlists: {ex.Message}");
                ViewBag.ErrorMessage = "Unable to load playlists from the server. Please try again later.";
                return View(new PagedResponse<PlaylistDto> {
                    Data = new List<PlaylistDto>(),
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalPages = 1,
                    TotalCount = 0
                });
            }
        }

        public async Task<IActionResult> Details(int id, string search = null)
        {
            try
            {
                var playlist = await _apiService.GetAsync<PlaylistDetailDto>($"api/Playlists/{id}");
                if (playlist == null)
                {
                    TempData["ErrorMessage"] = "Playlist not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Handle search if provided
                if (!string.IsNullOrWhiteSpace(search))
                {
                    try
                    {
                        var searchResults = await _apiService.GetAsync<List<SongDto>>($"api/Search/songs?query={Uri.EscapeDataString(search)}");
                        ViewBag.SearchResults = searchResults ?? new List<SongDto>();
                        ViewBag.SearchQuery = search;
                    }
                    catch (Exception searchEx)
                    {
                        Console.WriteLine($"Search error: {searchEx.Message}");
                        ViewBag.SearchError = "Failed to search for songs. Please try again.";
                        ViewBag.SearchResults = new List<SongDto>();
                        ViewBag.SearchQuery = search;
                    }
                }

                // If we have any temporary messages, pass them to the view
                if (TempData["SuccessMessage"] != null)
                {
                    ViewBag.SuccessMessage = TempData["SuccessMessage"];
                }
                if (TempData["ErrorMessage"] != null)
                {
                    ViewBag.ErrorMessage = TempData["ErrorMessage"];
                }

                return View(playlist);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving playlist details: {ex.Message}");
                TempData["ErrorMessage"] = "Unable to load playlist details. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddSongToPlaylist(int playlistId, int songId)
        {
            if (playlistId <= 0 || songId <= 0)
            {
                TempData["ErrorMessage"] = "Invalid playlist or song ID.";
                return RedirectToAction(nameof(Details), new { id = playlistId });
            }

            try
            {
                await _apiService.PostAsync<object>($"api/Playlists/{playlistId}/songs", new { songId });
                TempData["SuccessMessage"] = "Song successfully added to playlist.";
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("409"))
            {
                TempData["ErrorMessage"] = "This song is already in the playlist.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding song to playlist: {ex.Message}");
                TempData["ErrorMessage"] = "Failed to add song to playlist. Please try again.";
            }
            return RedirectToAction(nameof(Details), new { id = playlistId });
        }

        public IActionResult Create()
        {
            // Ensure TempData error messages are passed to the view
            if (TempData["ErrorMessage"] != null)
            {
                ViewBag.ErrorMessage = TempData["ErrorMessage"];
            }
            return View();
        }
        
        [HttpPost]
        public async Task<IActionResult> Create(PlaylistDto model, IFormFile coverImage)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var result = await _apiService.PostAsync<PlaylistDto>("api/Playlists", model);
                    if (result != null)
                    {
                        // Handle cover image upload if file is provided
                        if (coverImage != null && coverImage.Length > 0)
                        {
                            // Generate filename based on playlist name or use a default
                            var fileName = !string.IsNullOrEmpty(result.Name) 
                                ? $"{result.Name.Replace(" ", "-").ToLower()}.{Path.GetExtension(coverImage.FileName).TrimStart('.')}"
                                : $"playlist-{result.Id}.{Path.GetExtension(coverImage.FileName).TrimStart('.')}";
                            
                            var uploadResult = await _fileUploadService.UploadPlaylistCoverAsync(coverImage, fileName, result.Name);
                            if (uploadResult.Success)
                            {
                                // Generate the API URL for the uploaded file
                                result.CoverImageUrl = $"https://localhost:5117/api/files/playlists/{fileName}";
                                // Update playlist with new cover image
                                await _apiService.PutAsync<PlaylistDto>($"api/Playlists/{result.Id}", result);
                            }
                        }
                        return RedirectToAction(nameof(Details), new { id = result.Id });
                    }
                    else
                    {
                        ModelState.AddModelError("", "Failed to create playlist. Please try again.");
                    }
                }
                catch (Exception ex)
                {
                    // Log the error
                    Console.WriteLine($"Error creating playlist: {ex.Message}");
                    ModelState.AddModelError("", "An error occurred while creating the playlist. Please try again.");
                }
            }
            // Pass error message to view if any
            return View(model);
        }

        // GET: Playlists/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var playlist = await _apiService.GetAsync<PlaylistDto>($"api/Playlists/{id}");
                if (playlist == null)
                {
                    TempData["ErrorMessage"] = "Playlist not found.";
                    return RedirectToAction(nameof(Index));
                }
                return View(playlist);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading playlist for edit: {ex.Message}");
                TempData["ErrorMessage"] = "Unable to load playlist for editing. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Playlists/Edit/5
        [HttpPost]
        public async Task<IActionResult> Edit(int id, PlaylistDto model, IFormFile coverImage)
        {
            if (id != model.Id)
            {
                TempData["ErrorMessage"] = "Invalid playlist ID.";
                return RedirectToAction(nameof(Index));
            }
            if (ModelState.IsValid)
            {
                try
                {
                    var result = await _apiService.PutAsync<PlaylistDto>($"api/Playlists/{id}", model);
                    if (result != null)
                    {
                        // Handle cover image upload if file is provided
                        if (coverImage != null && coverImage.Length > 0)
                        {
                            // Generate filename based on playlist name or use a default
                            var fileName = !string.IsNullOrEmpty(result.Name) 
                                ? $"{result.Name.Replace(" ", "-").ToLower()}.{Path.GetExtension(coverImage.FileName).TrimStart('.')}"
                                : $"playlist-{id}.{Path.GetExtension(coverImage.FileName).TrimStart('.')}";
                            
                            var uploadResult = await _fileUploadService.UploadPlaylistCoverAsync(coverImage, fileName, result.Name);
                            if (uploadResult.Success)
                            {
                                // Generate the API URL for the uploaded file
                                result.CoverImageUrl = $"https://localhost:5117/api/files/playlists/{fileName}";
                                // Update playlist with new cover image
                                await _apiService.PutAsync<PlaylistDto>($"api/Playlists/{id}", result);
                            }
                        }
                        return RedirectToAction(nameof(Details), new { id = result.Id });
                    }
                    else
                    {
                        ModelState.AddModelError("", "Failed to update playlist. Please try again.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error editing playlist: {ex.Message}");
                    ModelState.AddModelError("", "An error occurred while editing the playlist. Please try again.");
                }
            }
            return View(model);
        }

        // GET: Playlists/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var playlist = await _apiService.GetAsync<PlaylistDto>($"api/Playlists/{id}");
                if (playlist == null)
                {
                    TempData["ErrorMessage"] = "Playlist not found.";
                    return RedirectToAction(nameof(Index));
                }
                return View(playlist);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading playlist for delete: {ex.Message}");
                TempData["ErrorMessage"] = "Unable to load playlist for deletion. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Playlists/Delete/5
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _apiService.DeleteAsync($"api/Playlists/{id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting playlist: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while deleting the playlist. Please try again.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

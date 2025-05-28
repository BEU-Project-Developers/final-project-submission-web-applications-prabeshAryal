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
    public class PlaylistsController : BaseAppController
    {
        private readonly FileUploadService _fileUploadService;

        public PlaylistsController(ApiService apiService, FileUploadService fileUploadService, ILogger<PlaylistsController> logger)
            : base(apiService, logger)
        {
            _fileUploadService = fileUploadService;
        }        public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
        {
            var emptyResponse = new PagedResponse<PlaylistDto> {
                Data = new List<PlaylistDto>(),
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = 1,
                TotalCount = 0
            };

            var response = await SafeApiCall(
                async () => await _apiService.GetAsync<PagedResponse<PlaylistDto>>($"api/Playlists?page={page}&pageSize={pageSize}"),
                emptyResponse,
                GetStandardErrorMessage("load", "playlists"),
                "PlaylistsController.Index"
            );

            return View(response);
        }        public async Task<IActionResult> Details(int id, string search = null)
        {
            return await SafeApiAction(
                async () =>
                {
                    var playlist = await _apiService.GetAsync<PlaylistDetailDto>($"api/Playlists/{id}");
                    if (playlist == null)
                    {
                        SetErrorMessage("Playlist not found.");
                        return RedirectToAction(nameof(Index));
                    }

                    // Handle search if provided
                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        var searchResults = await SafeApiCall(
                            async () => await _apiService.GetAsync<List<SongDto>>($"api/Search/songs?query={Uri.EscapeDataString(search)}"),
                            new List<SongDto>(),
                            "Failed to search for songs",
                            $"PlaylistsController.Details search for '{search}'"
                        );
                        
                        ViewBag.SearchResults = searchResults;
                        ViewBag.SearchQuery = search;
                        if (searchResults.Count == 0)
                        {
                            ViewBag.SearchError = "No songs found matching your search.";
                        }
                    }

                    return View(playlist);
                },
                () => {
                    SetErrorMessage(GetStandardErrorMessage("load", "playlist details"));
                    return RedirectToAction(nameof(Index));
                },
                GetStandardErrorMessage("load", "playlist details"),
                $"PlaylistsController.Details for ID {id}"
            );
        }        [HttpPost]
        public async Task<IActionResult> AddSongToPlaylist(int playlistId, int songId)
        {
            if (playlistId <= 0 || songId <= 0)
            {
                SetErrorMessage("Invalid playlist or song ID.");
                return RedirectToAction(nameof(Details), new { id = playlistId });
            }

            await SafeApiCall(
                async () =>
                {
                    await _apiService.PostAsync<object>($"api/Playlists/{playlistId}/songs", new { songId });
                    SetSuccessMessage("Song successfully added to playlist.");
                    return true;
                },
                false,
                "Failed to add song to playlist. This song may already be in the playlist.",
                $"PlaylistsController.AddSongToPlaylist - playlist {playlistId}, song {songId}"
            );

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
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            return await SafeApiAction(
                async () =>
                {
                    var result = await _apiService.PostAsync<PlaylistDto>("api/Playlists", model);
                    if (result != null)
                    {
                        // Handle cover image upload if file is provided
                        if (coverImage != null && coverImage.Length > 0)
                        {
                            var fileName = !string.IsNullOrEmpty(result.Name) 
                                ? $"{result.Name.Replace(" ", "-").ToLower()}.{Path.GetExtension(coverImage.FileName).TrimStart('.')}"
                                : $"playlist-{result.Id}.{Path.GetExtension(coverImage.FileName).TrimStart('.')}";
                            
                            var uploadResult = await _fileUploadService.UploadPlaylistCoverAsync(coverImage, fileName, result.Name);
                            if (uploadResult.Success)
                            {
                                result.CoverImageUrl = $"https://localhost:5117/api/files/playlists/{fileName}";
                                await _apiService.PutAsync<PlaylistDto>($"api/Playlists/{result.Id}", result);
                            }
                        }
                        SetSuccessMessage("Playlist created successfully.");
                        return RedirectToAction(nameof(Details), new { id = result.Id });
                    }
                    else
                    {
                        ViewBag.ErrorMessage = "Failed to create playlist.";
                        return View(model);
                    }
                },
                () => View(model),
                GetStandardErrorMessage("create", "playlist"),
                "PlaylistsController.Create POST"
            );
        }        // GET: Playlists/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            return await SafeApiAction(
                async () =>
                {
                    var playlist = await _apiService.GetAsync<PlaylistDto>($"api/Playlists/{id}");
                    if (playlist == null)
                    {
                        return NotFound();
                    }
                    return View(playlist);
                },
                () => {
                    SetErrorMessage(GetStandardErrorMessage("load", "playlist"));
                    return RedirectToAction(nameof(Index));
                },
                GetStandardErrorMessage("load", "playlist"),
                $"PlaylistsController.Edit GET for ID {id}"
            );
        }        // POST: Playlists/Edit/5
        [HttpPost]
        public async Task<IActionResult> Edit(int id, PlaylistDto model, IFormFile coverImage)
        {
            if (id != model.Id)
            {
                SetErrorMessage("Invalid playlist ID.");
                return RedirectToAction(nameof(Index));
            }
            
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            return await SafeApiAction(
                async () =>
                {
                    var result = await _apiService.PutAsync<PlaylistDto>($"api/Playlists/{id}", model);
                    if (result != null)
                    {
                        // Handle cover image upload if file is provided
                        if (coverImage != null && coverImage.Length > 0)
                        {
                            var fileName = !string.IsNullOrEmpty(result.Name) 
                                ? $"{result.Name.Replace(" ", "-").ToLower()}.{Path.GetExtension(coverImage.FileName).TrimStart('.')}"
                                : $"playlist-{id}.{Path.GetExtension(coverImage.FileName).TrimStart('.')}";
                            
                            var uploadResult = await _fileUploadService.UploadPlaylistCoverAsync(coverImage, fileName, result.Name);
                            if (uploadResult.Success)
                            {
                                result.CoverImageUrl = $"https://localhost:5117/api/files/playlists/{fileName}";
                                await _apiService.PutAsync<PlaylistDto>($"api/Playlists/{id}", result);
                            }
                        }
                        SetSuccessMessage("Playlist updated successfully.");
                        return RedirectToAction(nameof(Details), new { id = result.Id });
                    }
                    else
                    {
                        ViewBag.ErrorMessage = "Failed to update playlist.";
                        return View(model);
                    }
                },
                () => View(model),
                GetStandardErrorMessage("update", "playlist"),
                $"PlaylistsController.Edit POST for ID {id}"
            );
        }        // GET: Playlists/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            return await SafeApiAction(
                async () =>
                {
                    var playlist = await _apiService.GetAsync<PlaylistDto>($"api/Playlists/{id}");
                    if (playlist == null)
                    {
                        return NotFound();
                    }
                    return View(playlist);
                },
                () => {
                    SetErrorMessage(GetStandardErrorMessage("load", "playlist"));
                    return RedirectToAction(nameof(Index));
                },
                GetStandardErrorMessage("load", "playlist"),
                $"PlaylistsController.Delete GET for ID {id}"
            );
        }

        // POST: Playlists/Delete/5
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await SafeApiCall(
                async () =>
                {
                    await _apiService.DeleteAsync($"api/Playlists/{id}");
                    SetSuccessMessage("Playlist deleted successfully.");
                    return true;
                },
                false,
                GetStandardErrorMessage("delete", "playlist"),
                $"PlaylistsController.DeleteConfirmed for ID {id}"
            );

            return RedirectToAction(nameof(Index));
        }
    }
}

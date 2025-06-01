using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MusicApp.Models;
using MusicApp.Models.DTOs;
using MusicApp.Models.ViewModels;
using MusicApp.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MusicApp.Controllers
{    public class PlaylistsController : BaseAppController
    {
        private readonly FileUploadService _fileUploadService;

        public PlaylistsController(ApiService apiService, AuthService authService, FileUploadService fileUploadService, ILogger<PlaylistsController> logger)
            : base(apiService, authService, logger)
        {
            _fileUploadService = fileUploadService;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string view = "my")
        {
            var emptyResponse = new PagedResponse<PlaylistDto> {
                Data = new List<PlaylistDto>(),
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = 1,
                TotalCount = 0
            };

            string endpoint;
            if (view == "all" && User.IsInRole("Admin"))
            {
                endpoint = $"api/Playlists?page={page}&pageSize={pageSize}&adminViewAll=true";
                ViewBag.CurrentView = "all";
            }
            else if (view == "public")
            {
                endpoint = $"api/Playlists?page={page}&pageSize={pageSize}";
                ViewBag.CurrentView = "public";
            }
            else
            {
                endpoint = $"api/Playlists/user?page={page}&pageSize={pageSize}";
                ViewBag.CurrentView = "my";
            }

            var response = await SafeApiCall(
                async () => await _apiService.GetAsync<PagedResponse<PlaylistDto>>(endpoint),
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

                    // Map PlaylistDetailDto to PlaylistDetailsViewModel
                    var viewModel = new PlaylistDetailsViewModel
                    {
                        PlaylistId = playlist.Id,
                        Name = playlist.Name ?? string.Empty,
                        Description = playlist.Description ?? string.Empty,
                        CoverImageUrl = playlist.CoverImageUrl,
                        IsOwner = User.Identity?.Name == playlist.Username, // Assuming this is how we determine ownership
                        Songs = playlist.Songs.Select(s => new SongViewModel
                        {
                            SongId = s.SongId,
                            Title = s.Title ?? string.Empty,
                            ArtistName = s.ArtistName ?? string.Empty,
                            AlbumName = s.AlbumTitle ?? string.Empty,
                            Duration = (int)(s.Duration?.TotalSeconds ?? 0),
                            CoverImageUrl = s.CoverImageUrl,
                            SongFileUrl = s.AudioUrl
                        }).ToList()
                    };

                    return View(viewModel);
                },
                () => {
                    SetErrorMessage(GetStandardErrorMessage("load", "playlist details"));
                    return RedirectToAction(nameof(Index));
                },
                GetStandardErrorMessage("load", "playlist details"),
                $"PlaylistsController.Details for ID {id}"
            );
        }

        [HttpPost]
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
            // Remove coverImage validation errors since it's not part of the model
            // Check for different possible keys that might be causing validation issues
            var keysToRemove = new[] { "coverImage", "CoverImage", "CoverImageUrl" };
            foreach (var key in keysToRemove)
            {
                if (ModelState.ContainsKey(key))
                {
                    ModelState.Remove(key);
                    _logger.LogInformation("Removed ModelState key: {Key}", key);
                }
            }
            
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
                        {                            var fileName = !string.IsNullOrEmpty(result.Name) 
                                ? $"{result.Name.Replace(" ", "-").ToLower()}.{Path.GetExtension(coverImage.FileName).TrimStart('.')}"
                                : $"playlist-{result.Id}.{Path.GetExtension(coverImage.FileName).TrimStart('.')}";
                            
                            var uploadResult = await _fileUploadService.UploadPlaylistCoverAsync(coverImage, fileName, result.Name ?? "");
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
        }

        // GET: Playlists/Edit/5
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
        }

        // POST: Playlists/Edit/5
        [HttpPost]
        public async Task<IActionResult> Edit(int id, PlaylistDto model, IFormFile coverImage)
        {
            _logger.LogInformation("Edit POST called - ID: {Id}, Model: {@Model}, CoverImage: {CoverImagePresent}", 
                id, model, coverImage != null);
                  if (id != model.Id)
            {
                _logger.LogWarning("ID mismatch - URL ID: {UrlId}, Model ID: {ModelId}", id, model.Id);
                SetErrorMessage("Invalid playlist ID.");
                return RedirectToAction(nameof(Index));
            }
            
            // Remove coverImage validation errors since it's not part of the model
            // Check for different possible keys that might be causing validation issues
            var keysToRemove = new[] { "coverImage", "CoverImage", "CoverImageUrl" };
            foreach (var key in keysToRemove)
            {
                if (ModelState.ContainsKey(key))
                {
                    ModelState.Remove(key);
                    _logger.LogInformation("Removed ModelState key: {Key}", key);
                }            }
            
            // Debug: Log all ModelState keys before validation
            _logger.LogInformation("ModelState keys before validation: {Keys}", 
                string.Join(", ", ModelState.Keys));
            
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid: {@ModelStateErrors}", 
                    ModelState.Where(x => x.Value.Errors.Count > 0)
                    .ToDictionary(x => x.Key, x => x.Value.Errors.Select(e => e.ErrorMessage)));
                return View(model);
            }

            return await SafeApiAction(
                async () =>
                {                    // Create the update DTO with only the necessary fields
                    var updateDto = new
                    {
                        Name = model.Name,
                        Description = model.Description,
                        IsPublic = model.IsPublic
                    };                    _logger.LogInformation("Attempting to update playlist {Id} with data: {@UpdateDto}", id, updateDto);
                    var result = await _apiService.PutAsync<PlaylistDto>($"api/Playlists/{id}", updateDto);
                    _logger.LogInformation("API call result: {@Result}", result);
                    if (result != null)
                    {                        // Handle cover image upload if file is provided
                        if (coverImage != null && coverImage.Length > 0)
                        {
                            _logger.LogInformation("Uploading cover image for playlist {Id}", id);
                            
                            // Use the backend's dedicated cover upload endpoint
                            try
                            {
                                var coverUploadResult = await _apiService.UploadFileAsync($"api/Playlists/{id}/cover", coverImage, "file");
                                if (!string.IsNullOrEmpty(coverUploadResult))
                                {
                                    _logger.LogInformation("Cover image uploaded successfully for playlist {Id}. File path: {FilePath}", id, coverUploadResult);
                                }
                                else
                                {
                                    _logger.LogWarning("Cover image upload returned null/empty result for playlist {Id}", id);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to upload cover image for playlist {Id}: {ErrorMessage}", id, ex.Message);
                                // Don't fail the entire operation if cover upload fails
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
        }

        // GET: Playlists/Delete/5
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

        // POST: Playlists/Copy/5
        [HttpPost]
        public async Task<IActionResult> CopyPlaylist(int id)
        {
            await SafeApiCall(                async () =>
                {
                    var result = await _apiService.PostAsync<object>($"api/Playlists/{id}/copy", new {});
                    SetSuccessMessage("Playlist copied to your library successfully.");
                    return true;
                },
                false,
                "Failed to copy playlist. You may not have permission to copy this playlist.",
                $"PlaylistsController.CopyPlaylist for ID {id}"
            );

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetUserPlaylists()
        {
            var playlists = await SafeApiCall(
                async () => await _apiService.GetAsync<PagedResponse<PlaylistDto>>("api/Playlists/user"),
                new PagedResponse<PlaylistDto> { Data = new List<PlaylistDto>() },
                "Unable to load your playlists",
                "PlaylistsController.GetUserPlaylists"
            );

            return Json(playlists);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAlbumToPlaylist(int playlistId, [FromBody] AlbumSongsDto albumSongs)
        {
            if (playlistId <= 0 || albumSongs == null || albumSongs.SongIds == null || !albumSongs.SongIds.Any())
            {
                return BadRequest("Invalid request data.");
            }

            try
            {
                foreach (var songId in albumSongs.SongIds)
                {
                    await _apiService.PostAsync<object>($"api/Playlists/{playlistId}/songs", new { songId = int.Parse(songId) });
                }
                return Ok("All album songs added to playlist!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding album songs to playlist {PlaylistId}", playlistId);
                return BadRequest($"Error adding songs to playlist: {ex.Message}");
            }
        }        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveSongFromPlaylist([FromBody] RemoveSongRequest request)
        {
            if (request == null)
            {
                return BadRequest("Invalid request.");
            }

            // Convert string parameters to integers
            if (!int.TryParse(request.PlaylistId?.ToString(), out int playlistId) || 
                !int.TryParse(request.SongId?.ToString(), out int songId))
            {
                return BadRequest("Invalid playlist or song ID format.");
            }

            if (playlistId <= 0 || songId <= 0)
            {
                return BadRequest("Invalid playlist or song ID.");
            }

            var success = await SafeApiCall(
                async () =>
                {
                    await _apiService.DeleteAsync($"api/Playlists/{playlistId}/songs/{songId}");
                    return true;
                },
                false,
                "Failed to remove song from playlist.",
                $"PlaylistsController.RemoveSongFromPlaylist - playlist {playlistId}, song {songId}"
            );

            if (success)
            {
                return Ok(new { message = "Song removed successfully!" });
            }
            else
            {
                return BadRequest("Failed to remove song from playlist.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchSongs(string query, int limit = 20)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Json(new { results = new { songs = new List<object>() } });
            }

            var searchResults = await SafeApiCall(
                async () => await _apiService.GetAsync<object>($"api/Search?query={Uri.EscapeDataString(query)}&limit={limit}"),
                new { results = new { songs = new List<object>() } },
                "Failed to search for songs",
                $"PlaylistsController.SearchSongs for query '{query}'"
            );            return Json(searchResults);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSongToPlaylistAjax([FromBody] AddSongRequest request)
        {
            if (request == null)
            {
                return BadRequest("Invalid request.");
            }

            // Convert string parameters to integers
            if (!int.TryParse(request.PlaylistId?.ToString(), out int playlistId) || 
                !int.TryParse(request.SongId?.ToString(), out int songId))
            {
                return BadRequest("Invalid playlist or song ID format.");
            }

            if (playlistId <= 0 || songId <= 0)
            {
                return BadRequest("Invalid playlist or song ID.");
            }

            var success = await SafeApiCall(
                async () =>
                {
                    await _apiService.PostAsync<object>($"api/Playlists/{playlistId}/songs", new { songId });
                    return true;
                },
                false,
                "Failed to add song to playlist. This song may already be in the playlist.",
                $"PlaylistsController.AddSongToPlaylistAjax - playlist {playlistId}, song {songId}"
            );

            if (success)
            {
                return Ok(new { message = "Song successfully added to playlist." });
            }
            else
            {
                return BadRequest("Failed to add song to playlist. This song may already be in the playlist.");
            }
        }
    }

    public class AlbumSongsDto
    {
        public List<string> SongIds { get; set; }
    }

    public class RemoveSongRequest
    {
        public object PlaylistId { get; set; }
        public object SongId { get; set; }
    }

    public class AddSongRequest
    {
        public object PlaylistId { get; set; }
        public object SongId { get; set; }
    }
}

using Microsoft.AspNetCore.Mvc;
using MusicApp.Models;
using MusicApp.Models.DTOs;
using MusicApp.Services;
using MusicApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace MusicApp.Controllers
{
    public class AlbumsController : BaseAppController
    {
        public AlbumsController(ApiService apiService, ILogger<AlbumsController> logger)
            : base(apiService, logger)
        {
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
        {
            var emptyResponse = new PagedResponse<AlbumDto>
            {
                Data = new List<AlbumDto>(),
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = 1,
                TotalCount = 0
            };

            var response = await SafeApiCall(
                async () => await _apiService.GetAsync<PagedResponse<AlbumDto>>($"api/Albums?page={page}&pageSize={pageSize}"),
                emptyResponse,
                GetStandardErrorMessage("load", "albums"),
                "AlbumsController.Index"
            );

            return View(response);
        }

        public async Task<IActionResult> Details(int id)
        {
            return await SafeApiAction(
                async () =>
                {
                    var album = await _apiService.GetAsync<AlbumDto>($"api/Albums/{id}");

                    if (album == null)
                    {
                        return NotFound();
                    }

                    return View(album);
                },
                () =>
                {
                    SetErrorMessage(GetStandardErrorMessage("load", "album details"));
                    return RedirectToAction(nameof(Index));
                },
                GetStandardErrorMessage("load", "album details"),
                $"AlbumsController.Details for ID {id}"
            );
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // Load artists for dropdown with safe API call
            var artists = await SafeApiCall(
                async () => await _apiService.GetAsync<PagedResponse<ArtistDto>>("api/Artists"),
                new PagedResponse<ArtistDto> { Data = new List<ArtistDto>() },
                "Unable to load artists",
                "AlbumsController.Create - loading artists"
            );

            ViewBag.Artists = artists?.Data ?? new List<ArtistDto>();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,ArtistId,ReleaseDate")] AlbumCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Reload artists for dropdown safely
                var artists = await SafeApiCall(
                    async () => await _apiService.GetAsync<PagedResponse<ArtistDto>>("api/Artists"),
                    new PagedResponse<ArtistDto> { Data = new List<ArtistDto>() },
                    "Unable to load artists",
                    "AlbumsController.Create POST - reloading artists on validation error"
                );
                ViewBag.Artists = artists?.Data ?? new List<ArtistDto>();
                return View(model);
            }

            return await SafeApiAction(
                async () =>
                {
                    // Map to backend DTO
                    var albumCreateDto = new
                    {
                        Title = model.Title,
                        ArtistId = model.ArtistId,
                        ReleaseDate = model.ReleaseDate
                    };

                    var result = await _apiService.PostAsync<object>("api/Albums", albumCreateDto);

                    if (result != null)
                    {
                        SetSuccessMessage("Album added successfully.");
                        return RedirectToAction("Index");
                    }

                    ViewBag.ErrorMessage = "Failed to add album.";
                    return await LoadArtistsAndReturnView(model);
                },
                () => LoadArtistsAndReturnView(model).Result,
                GetStandardErrorMessage("create", "album"),
                "AlbumsController.Create POST"
            );
        }

        private async Task<IActionResult> LoadArtistsAndReturnView(AlbumCreateViewModel model)
        {
            // Reload artists for dropdown on error
            var artists = await SafeApiCall(
                async () => await _apiService.GetAsync<PagedResponse<ArtistDto>>("api/Artists"),
                new PagedResponse<ArtistDto> { Data = new List<ArtistDto>() },
                "Unable to load artists",
                "AlbumsController - loading artists on error"
            );
            ViewBag.Artists = artists?.Data ?? new List<ArtistDto>();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            return await SafeApiAction(
                async () =>
                {
                    var album = await _apiService.GetAsync<AlbumDto>($"api/Albums/{id}");
                    if (album == null)
                    {
                        return NotFound();
                    }

                    // Load artists for dropdown
                    var artists = await SafeApiCall(
                        async () => await _apiService.GetAsync<PagedResponse<ArtistDto>>("api/Artists"),
                        new PagedResponse<ArtistDto> { Data = new List<ArtistDto>() },
                        "Unable to load artists",
                        $"AlbumsController.Edit GET - Loading artists for ID {id}"
                    );
                    ViewBag.Artists = artists?.Data ?? new List<ArtistDto>();

                    return View(album);
                },
                () => NotFound(),
                GetStandardErrorMessage("load", "album"),
                $"AlbumsController.Edit GET for ID {id}"
            );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AlbumDto model, IFormFile coverImage)
        {
            _logger.LogInformation("AlbumsController.Edit POST action hit with id: {Id} and model.Id: {ModelId}", id, model.Id);

            if (id != model.Id)
            {
                _logger.LogWarning("AlbumsController.Edit POST: id mismatch. Expected: {ExpectedId}, Actual: {ActualId}", id, model.Id);
                return BadRequest();
            }

            // Remove coverImage validation errors since it's not part of the model
            var keysToRemove = new[] { "coverImage", "CoverImage" };
            foreach (var key in keysToRemove)
            {
                if (ModelState.ContainsKey(key))
                {
                    ModelState.Remove(key);
                }
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("AlbumsController.Edit POST: ModelState is invalid");
                // Log ModelState errors
                foreach (var entry in ModelState.Values)
                {
                    foreach (var error in entry.Errors)
                    {
                        _logger.LogError("ModelState Error: {ErrorMessage}", error.ErrorMessage);
                    }
                }

                // Reload artists for dropdown on validation error
                var artists = await SafeApiCall(
                    async () => await _apiService.GetAsync<PagedResponse<ArtistDto>>("api/Artists"),
                    new PagedResponse<ArtistDto> { Data = new List<ArtistDto>() },
                    "Unable to load artists",
                    "AlbumsController.Edit POST - Loading artists on validation error"
                );
                ViewBag.Artists = artists?.Data ?? new List<ArtistDto>();
                return View(model);
            }

            return await SafeApiAction(
                async () =>
                {
                    _logger.LogInformation("AlbumsController.Edit POST: Attempting to update album with id: {Id}", id);
                    var updateDto = new AlbumUpdateDTO
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

                    // Handle cover image upload if file is provided
                    if (coverImage != null && coverImage.Length > 0)
                    {
                        try
                        {
                            // Use multipart form data for file upload
                            var filePath = await _apiService.UploadFileAsync($"api/Albums/{id}/cover", coverImage);
                            if (!string.IsNullOrEmpty(filePath))
                            {
                                _logger.LogInformation("Cover image uploaded successfully for album {Id}", id);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to upload cover image for album {Id}", id);
                            // Don't fail the entire operation if image upload fails
                        }
                    }

                    SetSuccessMessage("Album updated successfully.");
                    return RedirectToAction("Index");
                },
                () =>
                {
                    SetErrorMessage("Error updating album. Please try again.");
                    return View(model);
                },
                GetStandardErrorMessage("update", "album"),
                $"AlbumsController.Edit POST for ID {id}"
            );
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            return await SafeApiAction(
                async () =>
                {
                    var album = await _apiService.GetAsync<AlbumDto>($"api/Albums/{id}");
                    if (album == null)
                    {
                        return NotFound();
                    }
                    return View(album);
                },
                () => NotFound(),
                GetStandardErrorMessage("load", "album"),
                $"AlbumsController.Delete GET for ID {id}"
            );
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await SafeApiCall(
                async () =>
                {
                    await _apiService.DeleteAsync($"api/Albums/{id}");
                    SetSuccessMessage("Album deleted successfully.");
                    return true;
                },
                false,
                GetStandardErrorMessage("delete", "album"),
                $"AlbumsController.DeleteConfirmed for ID {id}"
            );

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Like(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Album ID cannot be null or empty.");
            }

            try
            {
                var result = await _apiService.PostAsync<object>($"api/Albums/{id}/like", new { });
                return Ok("Album liked successfully!");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error liking album {Id}: {Message}", id, ex.Message);
                return StatusCode(ex.StatusCode.HasValue ? (int)ex.StatusCode.Value : 500,
                    $"Failed to like album: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error liking album {Id}: {Message}", id, ex.Message);
                return StatusCode(500, $"Failed to like album: {ex.Message}");
            }
        }
    }
}

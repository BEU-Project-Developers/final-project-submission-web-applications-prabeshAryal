using Microsoft.AspNetCore.Mvc;
using MusicApp.Models;
using MusicApp.Models.DTOs;
using MusicApp.Services;
using MusicApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MusicApp.Controllers
{    public class AlbumsController : BaseAppController
    {
        public AlbumsController(ApiService apiService, ILogger<AlbumsController> logger)
            : base(apiService, logger)
        {
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
        {
            var emptyResponse = new PagedResponse<AlbumDto> {
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
        }        public async Task<IActionResult> Details(int id)
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
                () => {
                    SetErrorMessage(GetStandardErrorMessage("load", "album details"));
                    return RedirectToAction(nameof(Index));
                },
                GetStandardErrorMessage("load", "album details"),
                $"AlbumsController.Details for ID {id}"
            );
        }        [HttpGet]
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
        }        [HttpPost]
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
                    var albumCreateDto = new {
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
        }        [HttpGet]
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
                    return View(album);
                },
                () => NotFound(),
                GetStandardErrorMessage("load", "album"),
                $"AlbumsController.Edit GET for ID {id}"
            );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]        public async Task<IActionResult> Edit(int id, AlbumDto model)
        {        [HttpPost]
        [ValidateAntiForgeryToken]        public async Task<IActionResult> Edit(int id, AlbumDto model)
        {
            _logger.LogInformation("AlbumsController.Edit POST action hit with id: {Id} and model.Id: {ModelId}", id, model.Id);
            
            if (id != model.Id)
            {
                _logger.LogWarning("AlbumsController.Edit POST: id mismatch. Expected: {ExpectedId}, Actual: {ActualId}", id, model.Id);
                return BadRequest();
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
                    SetSuccessMessage("Album updated successfully.");
                    return RedirectToAction("Index");
                },
                () => View(model),
                GetStandardErrorMessage("update", "album"),
                $"AlbumsController.Edit POST for ID {id}"
            );
        }        [HttpGet]
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
    }
}

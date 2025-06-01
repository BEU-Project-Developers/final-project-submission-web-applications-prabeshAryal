using Microsoft.AspNetCore.Mvc;
using MusicApp.Models;
using MusicApp.Models.DTOs;
using MusicApp.Services;
using MusicApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MusicApp.Controllers
{    public class SongsController : BaseAppController
    {
        public SongsController(ApiService apiService, AuthService authService, ILogger<SongsController> logger)
            : base(apiService, authService, logger)
        {
        }        public async Task<IActionResult> Index(int page = 1, int pageSize = 0)
        {
            // Set default page size based on user role if not specified
            if (pageSize == 0)
            {
                pageSize = User.IsInRole("Admin") ? 20 : 21; // Admin: table view, User: 3 rows × 7 = 21
            }
            
            var emptyResponse = new PagedResponse<SongDto> {
                Data = new List<SongDto>(),
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = 1,
                TotalCount = 0
            };

            var response = await SafeApiCall(
                async () => await _apiService.GetAsync<PagedResponse<SongDto>>($"api/Songs?page={page}&pageSize={pageSize}"),
                emptyResponse,
                GetStandardErrorMessage("load", "songs"),
                "SongsController.Index"
            );

            return View(response);
        }public async Task<IActionResult> Details(int id)
        {
            return await SafeApiAction(
                async () =>
                {
                    var song = await _apiService.GetAsync<SongDto>($"api/Songs/{id}");
                    
                    if (song == null)
                    {
                        return NotFound();
                    }
                    
                    // Fetch similar songs
                    var similarSongs = await SafeApiCall(
                        async () => await _apiService.GetAsync<List<SongDto>>($"api/Songs/{id}/similar?limit=5"),
                        new List<SongDto>(),
                        "Unable to load similar songs",
                        $"SongsController.Details - Loading similar songs for ID {id}"
                    );
                    
                    ViewBag.SimilarSongs = similarSongs;
                    
                    return View(song);
                },
                () => {
                    SetErrorMessage(GetStandardErrorMessage("load", "song details"));
                    return RedirectToAction(nameof(Index));
                },
                GetStandardErrorMessage("load", "song details"),
                $"SongsController.Details for ID {id}"
            );
        }[HttpGet]
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
        public async Task<IActionResult> Create([Bind("Title,ArtistId,AlbumId,Duration,TrackNumber,Genre,ReleaseDate")] SongCreateViewModel model, int[] ArtistIds)
        {            if (!ModelState.IsValid)
            {
                var artists = await SafeApiCall(
                    async () => await _apiService.GetAsync<PagedResponse<ArtistDto>>("api/Artists"),
                    new PagedResponse<ArtistDto> { Data = new List<ArtistDto>() },
                    "Unable to load artists",
                    "SongsController.Create POST - Loading artists for validation error"
                );
                var albums = await SafeApiCall(
                    async () => await _apiService.GetAsync<PagedResponse<AlbumDto>>("api/Albums"),
                    new PagedResponse<AlbumDto> { Data = new List<AlbumDto>() },
                    "Unable to load albums",
                    "SongsController.Create POST - Loading albums for validation error"
                );
                ViewBag.Artists = artists?.Data ?? new List<ArtistDto>();
                ViewBag.Albums = albums?.Data ?? new List<AlbumDto>();
                return View(model);
            }            return await SafeApiAction(async () =>
            {
                // Get PrimaryArtistId from the form data
                int? primaryArtistId = null;
                if (Request.Form.ContainsKey("PrimaryArtistId") && int.TryParse(Request.Form["PrimaryArtistId"], out var parsedPrimaryId))
                {
                    primaryArtistId = parsedPrimaryId;
                }

                // Use the ArtistIds from the form submission
                // SOrry for songs
                var artistIds = ArtistIds?.ToList() ?? new List<int>();
                
                // Ensure primary artist is included in the list if not already present
                if (primaryArtistId.HasValue && !artistIds.Contains(primaryArtistId.Value))
                {
                    artistIds.Add(primaryArtistId.Value);
                }
                
                // Log received data for debugging
                _logger.LogInformation("Create song - Received ArtistIds: {ArtistIds}, PrimaryArtistId: {PrimaryArtistId}", 
                    string.Join(", ", artistIds), primaryArtistId);

                // Map to backend DTO with multiple artists support
                var songCreateDto = new SongCreateDTO
                {
                    Title = model.Title,
                    ArtistId = model.ArtistId, // Keep backward compatibility
                    ArtistIds = artistIds.Any() ? artistIds : (model.ArtistId.HasValue ? new List<int> { model.ArtistId.Value } : new List<int>()),
                    PrimaryArtistId = primaryArtistId ?? model.ArtistId,
                    AlbumId = model.AlbumId,
                    Duration = model.Duration,
                    TrackNumber = model.TrackNumber,
                    Genre = model.Genre,
                    ReleaseDate = model.ReleaseDate
                };
                
                var result = await _apiService.PostAsync<object>("api/Songs", songCreateDto);
                if (result != null)
                {
                    SetSuccessMessage("Song added successfully.");
                    return RedirectToAction("Index");
                }
                  SetErrorMessage("Failed to add song.");
                
                // Reload dropdowns on error
                var artists = await SafeApiCall(
                    async () => await _apiService.GetAsync<PagedResponse<ArtistDto>>("api/Artists"),
                    new PagedResponse<ArtistDto> { Data = new List<ArtistDto>() },
                    "Unable to load artists",
                    "SongsController.Create POST - Loading artists on error"
                );
                var albums = await SafeApiCall(
                    async () => await _apiService.GetAsync<PagedResponse<AlbumDto>>("api/Albums"),
                    new PagedResponse<AlbumDto> { Data = new List<AlbumDto>() },
                    "Unable to load albums",
                    "SongsController.Create POST - Loading albums on error"
                );
                ViewBag.Artists = artists?.Data ?? new List<ArtistDto>();
                ViewBag.Albums = albums?.Data ?? new List<AlbumDto>();
                
                return View(model);            },            () => {
                SetErrorMessage("Error creating song. Please try again.");
                
                // Set empty dropdowns on error fallback (cannot make async calls in fallback)
                ViewBag.Artists = new List<ArtistDto>();
                ViewBag.Albums = new List<AlbumDto>();
                
                return View(model);            },
            "Error creating song. Please try again.",
            "SongsController.Create POST");
        }        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            return await SafeApiAction(async () =>
            {
                var song = await SafeApiCall(
                    async () => await _apiService.GetAsync<SongDto>($"api/Songs/{id}"),
                    (SongDto)null,
                    "Unable to load song details",
                    $"SongsController.Edit GET for ID {id}"
                );
                
                if (song == null)
                {
                    return NotFound();
                }

                // Load artists and albums for dropdowns
                var artists = await SafeApiCall(
                    async () => await _apiService.GetAsync<PagedResponse<ArtistDto>>("api/Artists"),
                    new PagedResponse<ArtistDto> { Data = new List<ArtistDto>() },
                    "Unable to load artists",
                    "SongsController.Edit GET - Loading artists"
                );
                var albums = await SafeApiCall(
                    async () => await _apiService.GetAsync<PagedResponse<AlbumDto>>("api/Albums"),
                    new PagedResponse<AlbumDto> { Data = new List<AlbumDto>() },
                    "Unable to load albums",
                    "SongsController.Edit GET - Loading albums"
                );
                ViewBag.Artists = artists?.Data ?? new List<ArtistDto>();                ViewBag.Albums = albums?.Data ?? new List<AlbumDto>();

                return View(song);
            },
            () => {
                SetErrorMessage("Unable to load song details. Please try again later.");
                return RedirectToAction(nameof(Index));
            },
            "Unable to load song details. Please try again later.",
            $"SongsController.Edit GET for ID {id}");
        }        [HttpPost]
        [ValidateAntiForgeryToken]        public async Task<IActionResult> Edit(int id, SongDto model, IFormFile audioFile, IFormFile coverImage, int[] ArtistIds)
        {
            _logger.LogInformation("SongsController.Edit POST action hit with id: {Id} and model.Id: {ModelId}", id, model.Id);
            
            // Log received ArtistIds for debugging
            _logger.LogInformation("Received ArtistIds: {ArtistIds}", string.Join(", ", ArtistIds ?? new int[0]));
            
            if (id != model.Id)
            {
                _logger.LogWarning("SongsController.Edit POST: id mismatch. Expected: {ExpectedId}, Actual: {ActualId}", id, model.Id);
                return BadRequest();
            }

            // Remove file validation errors since they're not part of the model
            var keysToRemove = new[] { "audioFile", "AudioFile", "coverImage", "CoverImage", "ArtistIds" };
            foreach (var key in keysToRemove)
            {
                if (ModelState.ContainsKey(key))
                {
                    ModelState.Remove(key);
                }
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("SongsController.Edit POST: ModelState is invalid");
                // Log ModelState errors
                foreach (var entry in ModelState.Values)
                {
                    foreach (var error in entry.Errors)
                    {
                        _logger.LogError("ModelState Error: {ErrorMessage}", error.ErrorMessage);
                    }
                }

                // Reload dropdowns for validation error
                var artists = await SafeApiCall(
                    async () => await _apiService.GetAsync<PagedResponse<ArtistDto>>("api/Artists"),
                    new PagedResponse<ArtistDto> { Data = new List<ArtistDto>() },
                    "Unable to load artists",
                    "SongsController.Edit POST - Loading artists on validation error"
                );
                var albums = await SafeApiCall(
                    async () => await _apiService.GetAsync<PagedResponse<AlbumDto>>("api/Albums"),
                    new PagedResponse<AlbumDto> { Data = new List<AlbumDto>() },
                    "Unable to load albums",
                    "SongsController.Edit POST - Loading albums on validation error"
                );
                ViewBag.Artists = artists?.Data ?? new List<ArtistDto>();
                ViewBag.Albums = albums?.Data ?? new List<AlbumDto>();
                return View(model);
            }

            return await SafeApiAction(async () =>
            {                _logger.LogInformation("SongsController.Edit POST: Attempting to update song with id: {Id}", id);
                
                // Get PrimaryArtistId from the form data
                int? primaryArtistId = null;
                if (Request.Form.ContainsKey("PrimaryArtistId") && int.TryParse(Request.Form["PrimaryArtistId"], out var parsedPrimaryId))
                {
                    primaryArtistId = parsedPrimaryId;
                }
                
                // Use the ArtistIds from the form submission
                var artistIds = ArtistIds?.ToList() ?? new List<int>();
                
                // Ensure primary artist is included in the list if not already present
                if (primaryArtistId.HasValue && !artistIds.Contains(primaryArtistId.Value))
                {
                    artistIds.Add(primaryArtistId.Value);
                }
                
                _logger.LogInformation("Final ArtistIds for update: {ArtistIds}, PrimaryArtistId: {PrimaryArtistId}", 
                    string.Join(", ", artistIds), primaryArtistId);
                
                var updateDto = new SongUpdateDTO
                {
                    Title = model.Title,
                    ArtistId = model.ArtistId, // Keep backward compatibility
                    ArtistIds = artistIds,
                    PrimaryArtistId = primaryArtistId ?? model.ArtistId,
                    AlbumId = model.AlbumId,
                    Duration = model.Duration,
                    AudioUrl = model.AudioUrl,
                    CoverImageUrl = model.CoverImageUrl,
                    TrackNumber = model.TrackNumber,
                    Genre = model.Genre,
                    ReleaseDate = model.ReleaseDate,
                    PlayCount = model.PlayCount
                };

                await _apiService.PutAsync<object>($"api/Songs/{id}", updateDto);
                
                // Handle audio file upload if file is provided
                if (audioFile != null && audioFile.Length > 0)
                {                    try
                    {
                        // Use multipart form data for file upload
                        var filePath = await _apiService.UploadFileAsync($"api/Songs/{id}/audio", audioFile);
                        if (!string.IsNullOrEmpty(filePath))
                        {
                            _logger.LogInformation("Audio file uploaded successfully for song {Id}", id);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to upload audio file for song {Id}", id);
                        // Don't fail the entire operation if audio upload fails
                    }
                }

                // Handle cover image upload if file is provided
                if (coverImage != null && coverImage.Length > 0)
                {                    try
                    {
                        // Use multipart form data for file upload
                        var filePath = await _apiService.UploadFileAsync($"api/Songs/{id}/cover", coverImage);
                        if (!string.IsNullOrEmpty(filePath))
                        {
                            _logger.LogInformation("Cover image uploaded successfully for song {Id}", id);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to upload cover image for song {Id}", id);
                        // Don't fail the entire operation if image upload fails
                    }
                }
                
                SetSuccessMessage("Song updated successfully.");
                return RedirectToAction("Index");            },
            () => {
                SetErrorMessage("Error updating song. Please try again.");
                return View(model);
            },
            "Error updating song. Please try again.",
            $"SongsController.Edit POST for ID {id}");
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {            return await SafeApiAction(async () =>
            {
                var song = await SafeApiCall(
                    async () => await _apiService.GetAsync<SongDto>($"api/Songs/{id}"),
                    (SongDto)null,
                    "Unable to load song details",
                    $"SongsController.Delete GET for ID {id}"
                );
                
                if (song == null)
                {
                    return NotFound();
                }
                
                return View(song);            },
            () => {
                SetErrorMessage("Unable to load song details for deletion.");
                return RedirectToAction("Index");
            },
            "Unable to load song details for deletion.",
            $"SongsController.Delete GET for ID {id}");
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            return await SafeApiAction(async () =>
            {
                await _apiService.DeleteAsync($"api/Songs/{id}");
                SetSuccessMessage("Song deleted successfully.");
                return RedirectToAction("Index");            },
            () => {
                SetErrorMessage("Error deleting song. Please try again.");
                return RedirectToAction("Index");
            },
            "Error deleting song. Please try again.",
            $"SongsController.DeleteConfirmed for ID {id}");
        }
    }
}
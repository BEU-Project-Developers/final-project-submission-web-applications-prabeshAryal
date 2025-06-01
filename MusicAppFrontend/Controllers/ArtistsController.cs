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
{
    public class ArtistsController : BaseAppController
    {
        public ArtistsController(ApiService apiService, AuthService authService, ILogger<ArtistsController> logger)
            : base(apiService, authService, logger)
        {
        }public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
        {
            var emptyResponse = new PagedResponse<ArtistDto> {
                Data = new List<ArtistDto>(),
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = 1,
                TotalCount = 0
            };

            var response = await SafeApiCall(
                async () => await _apiService.GetAsync<PagedResponse<ArtistDto>>($"api/Artists?page={page}&pageSize={pageSize}"),
                emptyResponse,
                GetStandardErrorMessage("load", "artists"),
                "ArtistsController.Index"
            );

            return View(response);
        }        public async Task<IActionResult> Details(int id)
        {
            return await SafeApiAction(
                async () =>
                {
                    var artist = await _apiService.GetAsync<ArtistDto>($"api/Artists/{id}");
                    
                    if (artist == null)
                    {
                        return NotFound();
                    }
                    
                    return View(artist);
                },
                () => {
                    SetErrorMessage(GetStandardErrorMessage("load", "artist details"));
                    return RedirectToAction(nameof(Index));
                },
                GetStandardErrorMessage("load", "artist details"),
                $"ArtistsController.Details for ID {id}"
            );
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Bio,Country,Genre,FormedDate,MonthlyListeners")] ArtistCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            return await SafeApiAction(
                async () =>
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
                        SetSuccessMessage("Artist added successfully.");
                        return RedirectToAction("Index");
                    }
                    
                    ViewBag.ErrorMessage = "Failed to add artist.";
                    return View(model);
                },
                () => View(model),
                GetStandardErrorMessage("create", "artist"),
                "ArtistsController.Create POST"
            );
        }        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            return await SafeApiAction(
                async () =>
                {
                    var artist = await _apiService.GetAsync<ArtistDto>($"api/Artists/{id}");
                    if (artist == null)
                    {
                        return NotFound();
                    }
                    return View(artist);
                },
                () => NotFound(),
                GetStandardErrorMessage("load", "artist"),
                $"ArtistsController.Edit GET for ID {id}"
            );
        }        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ArtistDto model, IFormFile profileImage)
        {
            _logger.LogInformation("ArtistsController.Edit POST action hit with id: {Id} and model.Id: {ModelId}", id, model.Id);
            
            if (id != model.Id)
            {
                _logger.LogWarning("ArtistsController.Edit POST: id mismatch. Expected: {ExpectedId}, Actual: {ActualId}", id, model.Id);
                return BadRequest();
            }

            // Remove file validation errors since they're not part of the model
            var keysToRemove = new[] { "profileImage", "ProfileImage" };
            foreach (var key in keysToRemove)
            {
                if (ModelState.ContainsKey(key))
                {
                    ModelState.Remove(key);
                }
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ArtistsController.Edit POST: ModelState is invalid");
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
                    _logger.LogInformation("ArtistsController.Edit POST: Attempting to update artist with id: {Id}", id);
                    var updateDto = new ArtistUpdateDTO
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
                    
                    // Handle profile image upload if file is provided
                    if (profileImage != null && profileImage.Length > 0)
                    {
                        try
                        {
                            // Use multipart form data for file upload
                            var filePath = await _apiService.UploadFileAsync($"api/Artists/{id}/image", profileImage);
                            if (!string.IsNullOrEmpty(filePath))
                            {
                                _logger.LogInformation("Profile image uploaded successfully for artist {Id}", id);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to upload profile image for artist {Id}", id);
                            // Don't fail the entire operation if image upload fails
                        }
                    }
                    
                    SetSuccessMessage("Artist updated successfully.");
                    return RedirectToAction("Index");
                },
                () => {
                    SetErrorMessage("Error updating artist. Please try again.");
                    return View(model);
                },
                GetStandardErrorMessage("update", "artist"),
                $"ArtistsController.Edit POST for ID {id}"
            );
        }[HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            return await SafeApiAction(
                async () =>
                {
                    var artist = await _apiService.GetAsync<ArtistDto>($"api/Artists/{id}");
                    if (artist == null)
                    {
                        return NotFound();
                    }
                    return View(artist);
                },
                () => NotFound(),
                GetStandardErrorMessage("load", "artist"),
                $"ArtistsController.Delete GET for ID {id}"
            );
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await SafeApiCall(
                async () =>
                {
                    await _apiService.DeleteAsync($"api/Artists/{id}");
                    SetSuccessMessage("Artist deleted successfully.");
                    return true;
                },
                false,
                GetStandardErrorMessage("delete", "artist"),
                $"ArtistsController.DeleteConfirmed for ID {id}"
            );

            return RedirectToAction("Index");
        }
    }
}
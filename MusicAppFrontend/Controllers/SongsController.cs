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
{
    public class SongsController : Controller
    {
        private readonly ApiService _apiService;

        public SongsController(ApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
        {
            try
            {
                var response = await _apiService.GetAsync<PagedResponse<SongDto>>($"api/Songs?page={page}&pageSize={pageSize}");
                if (response == null)
                {
                    response = new PagedResponse<SongDto> {
                        Data = new List<SongDto>(),
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
                Console.WriteLine($"Error retrieving songs: {ex.Message}");
                ViewBag.ErrorMessage = "Unable to load songs from the server. Please try again later.";
                return View(new PagedResponse<SongDto> {
                    Data = new List<SongDto>(),
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
        }        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var song = await _apiService.GetAsync<SongDto>($"api/Songs/{id}");
                if (song == null)
                {
                    return NotFound();
                }

                // Load artists and albums for dropdowns
                var artists = await _apiService.GetAsync<PagedResponse<ArtistDto>>("api/Artists");
                var albums = await _apiService.GetAsync<PagedResponse<AlbumDto>>("api/Albums");
                ViewBag.Artists = artists?.Data ?? new List<ArtistDto>();
                ViewBag.Albums = albums?.Data ?? new List<AlbumDto>();

                return View(song);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving song for edit: {ex.Message}");
                TempData["ErrorMessage"] = "Unable to load song details. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SongDto model)
        {
            Console.WriteLine($"Received song model for update: Id={model.Id}, Title={model.Title}, ArtistId={model.ArtistId}, AlbumId={model.AlbumId}, Duration={model.Duration}, AudioUrl={model.AudioUrl}, CoverImageUrl={model.CoverImageUrl}, TrackNumber={model.TrackNumber}, Genre={model.Genre}, ReleaseDate={model.ReleaseDate}, PlayCount={model.PlayCount}");

            if (!ModelState.IsValid)
            {
                foreach (var state in ModelState)
                {
                    if (state.Value.Errors.Any())
                    {
                        Console.WriteLine($"ModelState Error for {state.Key}: {string.Join(", ", state.Value.Errors.Select(e => e.ErrorMessage))}");
                    }
                }
                return View(model);
            }
            try
            {
                var updateDto = new SongUpdateDto
                {
                    Title = model.Title,
                    ArtistId = model.ArtistId,
                    AlbumId = model.AlbumId,
                    Duration = model.Duration,
                    AudioUrl = model.AudioUrl,
                    CoverImageUrl = model.CoverImageUrl,
                    TrackNumber = model.TrackNumber,
                    Genre = model.Genre,
                    ReleaseDate = model.ReleaseDate,
                    PlayCount = model.PlayCount
                };
                
                Console.WriteLine($"Sending SongUpdateDto to backend: Title={updateDto.Title}, ArtistId={updateDto.ArtistId}, AlbumId={updateDto.AlbumId}, Duration={updateDto.Duration}, AudioUrl={updateDto.AudioUrl}, CoverImageUrl={updateDto.CoverImageUrl}, TrackNumber={updateDto.TrackNumber}, Genre={updateDto.Genre}, ReleaseDate={updateDto.ReleaseDate}, PlayCount={updateDto.PlayCount}");

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
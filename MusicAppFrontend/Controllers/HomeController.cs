using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MusicApp.Models;
using MusicApp.Models.DTOs;
using MusicApp.Services;
using System.Threading.Tasks;
using MusicApp.ViewModels;
using System.Linq;
using System.Text.Json;

namespace MusicApp.Controllers
{
    public class HomeController : BaseAppController
    {
        public HomeController(ApiService apiService, AuthService authService, ILogger<HomeController> logger)
            : base(apiService, authService, logger)
        {
        }

        public async Task<IActionResult> Index()
        {
            return await SafeApiAction(async () =>
            {
                var viewModel = new HomeViewModel();

                // Get featured artist (most popular artist)
                var artistsResponse = await SafeApiCall(
                    () => _apiService.GetAsync<PaginatedResponse<ArtistViewModel>>("api/Artists"),
                    new PaginatedResponse<ArtistViewModel> { Data = new List<ArtistViewModel>() },
                    "Unable to load featured artists at this time",
                    "HomeController.Index - Loading artists"
                );                if (artistsResponse?.Data != null && artistsResponse.Data.Any())
                {
                    // Get top 5 artists by monthly listeners and randomly select one to feature
                    var top5Artists = artistsResponse.Data.OrderByDescending(a => a.MonthlyListeners).Take(10).ToList();
                    if (top5Artists.Any())
                    {
                        var random = new Random();
                        var randomIndex = random.Next(0, top5Artists.Count);
                        viewModel.FeaturedArtist = top5Artists[randomIndex];
                    }
                }

                // Get latest albums
                var albumsResponse = await SafeApiCall(
                    () => _apiService.GetAsync<PaginatedResponse<AlbumViewModel>>("api/Albums"),
                    new PaginatedResponse<AlbumViewModel> { Data = new List<AlbumViewModel>() },
                    "Unable to load latest albums at this time",
                    "HomeController.Index - Loading albums"
                );

                if (albumsResponse?.Data != null)
                {
                    viewModel.LatestAlbums = albumsResponse.Data.OrderByDescending(a => a.Year ?? 0).Take(8).ToList();
                }

                // Get popular playlists
                var playlistsResponse = await SafeApiCall(
                    () => _apiService.GetAsync<PaginatedResponse<PlaylistViewModel>>("api/Playlists"),
                    new PaginatedResponse<PlaylistViewModel> { Data = new List<PlaylistViewModel>() },
                    "Unable to load popular playlists at this time",
                    "HomeController.Index - Loading playlists"
                );

                if (playlistsResponse?.Data != null)
                {
                    viewModel.PopularPlaylists = playlistsResponse.Data.OrderByDescending(p => p.SongCount).Take(8).ToList();
                }

                // Get recent songs
                var songsResponse = await SafeApiCall(
                    () => _apiService.GetAsync<PaginatedResponse<SongViewModel>>("api/Songs"),
                    new PaginatedResponse<SongViewModel> { Data = new List<SongViewModel>() },
                    "Unable to load recent songs at this time",
                    "HomeController.Index - Loading songs"
                );

                if (songsResponse?.Data != null)
                {
                    viewModel.RecentSongs = songsResponse.Data.Take(10).ToList();
                }

                return View(viewModel);
            }, () => View(new HomeViewModel()), 
            "Unable to load the home page at this time",
            "HomeController.Index");
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public async Task<IActionResult> SearchResults(string q)
        {
            ViewBag.SearchQuery = q;

            if (string.IsNullOrWhiteSpace(q))
            {
                return View(new SearchResultsDto());
            }

            return await SafeApiAction(async () =>
            {
                // Fetch raw JSON from backend
                var client = await _apiService.GetHttpClientAsync();
                var response = await client.GetAsync($"api/Search?query={Uri.EscapeDataString(q)}");
                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Unable to perform search at this time";
                    return View(new SearchResultsDto());
                }
                var content = await response.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(content);
                var root = doc.RootElement;
                if (root.TryGetProperty("results", out var resultsElem))
                {
                    var resultsDto = System.Text.Json.JsonSerializer.Deserialize<SearchResultsDto>(resultsElem.GetRawText(), new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return View(resultsDto ?? new SearchResultsDto());
                }
                // fallback: try to parse as direct SearchResultsDto
                var fallbackDto = System.Text.Json.JsonSerializer.Deserialize<SearchResultsDto>(content, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return View(fallbackDto ?? new SearchResultsDto());
            }, () => {
                ViewBag.Error = "An error occurred while searching. Please try again later.";
                return View(new SearchResultsDto());
            }, "Unable to perform search at this time",
            "HomeController.SearchResults");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}

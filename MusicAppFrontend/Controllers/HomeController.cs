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
    public class HomeController : Controller
    {
        private readonly ApiService _apiService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApiService apiService, ILogger<HomeController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new HomeViewModel();

            try
            {
                // Get featured artist (most popular artist)
                var artistsResponse = await _apiService.GetAsync<PaginatedResponse<ArtistViewModel>>("api/Artists");
                if (artistsResponse?.Data != null && artistsResponse.Data.Any())
                {
                    viewModel.FeaturedArtist = artistsResponse.Data.OrderByDescending(a => a.MonthlyListeners).First();
                }

                // Get latest albums
                var albumsResponse = await _apiService.GetAsync<PaginatedResponse<AlbumViewModel>>("api/Albums");
                if (albumsResponse?.Data != null)
                {
                    viewModel.LatestAlbums = albumsResponse.Data.OrderByDescending(a => a.Year ?? 0).Take(8).ToList();
                }

                // Get popular playlists
                var playlistsResponse = await _apiService.GetAsync<PaginatedResponse<PlaylistViewModel>>("api/Playlists");
                if (playlistsResponse?.Data != null)
                {
                    viewModel.PopularPlaylists = playlistsResponse.Data.OrderByDescending(p => p.SongCount).Take(8).ToList();
                }

                // Get recent songs
                var songsResponse = await _apiService.GetAsync<PaginatedResponse<SongViewModel>>("api/Songs");
                if (songsResponse?.Data != null)
                {
                    viewModel.RecentSongs = songsResponse.Data.Take(10).ToList();
                }
            }
            catch (JsonException ex)
            {
                // Log the JSON conversion error
                _logger.LogError(ex, "JSON conversion error: {ErrorMessage}", ex.Message);
                // Set default values for the view model
                viewModel.FeaturedArtist = null;
                viewModel.LatestAlbums = new List<AlbumViewModel>();
                viewModel.PopularPlaylists = new List<PlaylistViewModel>();
                viewModel.RecentSongs = new List<SongViewModel>();
                // Add a user-friendly error message
                TempData["Error"] = "Unable to load content. Please try again later.";
            }
            catch (Exception ex)
            {
                // Log other errors
                _logger.LogError(ex, "Error fetching home page data: {ErrorMessage}", ex.Message);
                // Set default values for the view model
                viewModel.FeaturedArtist = null;
                viewModel.LatestAlbums = new List<AlbumViewModel>();
                viewModel.PopularPlaylists = new List<PlaylistViewModel>();
                viewModel.RecentSongs = new List<SongViewModel>();
                // Add a user-friendly error message
                TempData["Error"] = "An error occurred while loading the page. Please try again later.";
            }

            return View(viewModel);
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

            try
            {
                var results = await _apiService.GetAsync<SearchResultsDto>($"api/Search?query={Uri.EscapeDataString(q)}");
                return View(results ?? new SearchResultsDto());
            }
            catch (Exception ex)
            {
                ViewBag.Error = "An error occurred while searching. Please try again later.";
                _logger.LogError(ex, "Search error: {ErrorMessage}", ex.Message);
                return View(new SearchResultsDto());
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}

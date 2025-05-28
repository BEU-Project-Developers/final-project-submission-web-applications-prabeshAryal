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
        public HomeController(ApiService apiService, ILogger<HomeController> logger)
            : base(apiService, logger)
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
                    new PaginatedResponse<ArtistViewModel> { Data = new List<ArtistViewModel>() }
                );

                if (artistsResponse?.Data != null && artistsResponse.Data.Any())
                {
                    viewModel.FeaturedArtist = artistsResponse.Data.OrderByDescending(a => a.MonthlyListeners).First();
                }

                // Get latest albums
                var albumsResponse = await SafeApiCall(
                    () => _apiService.GetAsync<PaginatedResponse<AlbumViewModel>>("api/Albums"),
                    new PaginatedResponse<AlbumViewModel> { Data = new List<AlbumViewModel>() }
                );

                if (albumsResponse?.Data != null)
                {
                    viewModel.LatestAlbums = albumsResponse.Data.OrderByDescending(a => a.Year ?? 0).Take(8).ToList();
                }

                // Get popular playlists
                var playlistsResponse = await SafeApiCall(
                    () => _apiService.GetAsync<PaginatedResponse<PlaylistViewModel>>("api/Playlists"),
                    new PaginatedResponse<PlaylistViewModel> { Data = new List<PlaylistViewModel>() }
                );

                if (playlistsResponse?.Data != null)
                {
                    viewModel.PopularPlaylists = playlistsResponse.Data.OrderByDescending(p => p.SongCount).Take(8).ToList();
                }

                // Get recent songs
                var songsResponse = await SafeApiCall(
                    () => _apiService.GetAsync<PaginatedResponse<SongViewModel>>("api/Songs"),
                    new PaginatedResponse<SongViewModel> { Data = new List<SongViewModel>() }
                );

                if (songsResponse?.Data != null)
                {
                    viewModel.RecentSongs = songsResponse.Data.Take(10).ToList();
                }

                return View(viewModel);
            }, () => View(new HomeViewModel()));
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
                var results = await SafeApiCall(
                    () => _apiService.GetAsync<SearchResultsDto>($"api/Search?query={Uri.EscapeDataString(q)}"),
                    new SearchResultsDto()
                );

                return View(results);
            }, () => {
                ViewBag.Error = "An error occurred while searching. Please try again later.";
                return View(new SearchResultsDto());
            });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}

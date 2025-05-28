using Microsoft.AspNetCore.Mvc;
using MusicApp.Models.DTOs;
using MusicApp.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MusicApp.Controllers
{
    public class AdminFileUploadController : BaseAppController
    {
        private readonly FileUploadService _fileUploadService;

        public AdminFileUploadController(ApiService apiService, FileUploadService fileUploadService, ILogger<AdminFileUploadController> logger)
            : base(apiService, logger)        {
            _fileUploadService = fileUploadService;
        }

        // GET: AdminFileUpload
        public async Task<IActionResult> Index()
        {
            return await SafeApiAction(async () =>
            {
                // Load data for dropdowns
                var songs = await SafeApiCall(
                    async () => await _apiService.GetAsync<PagedResponse<SongDto>>("api/Songs?pageSize=1000"),
                    new PagedResponse<SongDto> { Data = new List<SongDto>() },
                    "Unable to load songs at this time",
                    "AdminFileUploadController.Index - Loading songs"
                );
                
                var albums = await SafeApiCall(
                    async () => await _apiService.GetAsync<PagedResponse<AlbumDto>>("api/Albums?pageSize=1000"),
                    new PagedResponse<AlbumDto> { Data = new List<AlbumDto>() },
                    "Unable to load albums at this time",
                    "AdminFileUploadController.Index - Loading albums"
                );
                
                var artists = await SafeApiCall(
                    async () => await _apiService.GetAsync<PagedResponse<ArtistDto>>("api/Artists?pageSize=1000"),
                    new PagedResponse<ArtistDto> { Data = new List<ArtistDto>() },
                    "Unable to load artists at this time",
                    "AdminFileUploadController.Index - Loading artists"
                );
                
                var playlists = await SafeApiCall(
                    async () => await _apiService.GetAsync<PagedResponse<PlaylistDto>>("api/Playlists?pageSize=1000"),
                    new PagedResponse<PlaylistDto> { Data = new List<PlaylistDto>() },
                    "Unable to load playlists at this time",
                    "AdminFileUploadController.Index - Loading playlists"
                );

                ViewBag.Songs = songs?.Data ?? new List<SongDto>();
                ViewBag.Albums = albums?.Data ?? new List<AlbumDto>();
                ViewBag.Artists = artists?.Data ?? new List<ArtistDto>();
                ViewBag.Playlists = playlists?.Data ?? new List<PlaylistDto>();
                
                return View();            },
            () => {
                SetErrorMessage("Error loading data for file upload interface.");
                ViewBag.Songs = new List<SongDto>();
                ViewBag.Albums = new List<AlbumDto>();
                ViewBag.Artists = new List<ArtistDto>();
                ViewBag.Playlists = new List<PlaylistDto>();
                return View();
            },
            "Unable to load file upload interface",
            "AdminFileUploadController.Index");
        }

        // POST: AdminFileUpload/UploadSongAudio
        [HttpPost]
        public async Task<IActionResult> UploadSongAudio(int songId, IFormFile audioFile)
        {
            return await SafeApiAction(async () =>
            {
                if (audioFile == null || audioFile.Length == 0)
                {
                    SetErrorMessage("Please select an audio file.");
                    return RedirectToAction("Index");
                }

                var result = await _fileUploadService.UploadSongAudioAsync(songId, audioFile);
                if (!string.IsNullOrEmpty(result))
                {
                    SetSuccessMessage("Audio file uploaded successfully.");
                }
                else
                {
                    SetErrorMessage("Failed to upload audio file.");
                }                return RedirectToAction("Index");
            },
            () => {
                SetErrorMessage("Error uploading audio file. Please try again.");
                return RedirectToAction("Index");
            },
            "Unable to upload audio file",
            "AdminFileUploadController.UploadSongAudio");
        }        // POST: AdminFileUpload/UploadSongCover
        [HttpPost]
        public async Task<IActionResult> UploadSongCover(int songId, IFormFile coverFile)
        {
            return await SafeApiAction(async () =>
            {
                if (coverFile == null || coverFile.Length == 0)
                {
                    SetErrorMessage("Please select a cover image.");
                    return RedirectToAction("Index");
                }

                var result = await _fileUploadService.UploadSongCoverAsync(songId, coverFile);
                if (!string.IsNullOrEmpty(result))
                {
                    SetSuccessMessage("Cover image uploaded successfully.");
                }
                else
                {
                    SetErrorMessage("Failed to upload cover image.");
                }                return RedirectToAction("Index");
            },
            () => {
                SetErrorMessage("Error uploading cover image. Please try again.");
                return RedirectToAction("Index");
            },
            "Unable to upload cover image",
            "AdminFileUploadController.UploadSongCover");
        }

        // POST: AdminFileUpload/UploadSongFiles - Upload both audio and cover for a song
        [HttpPost]
        public async Task<IActionResult> UploadSongFiles(int songId, IFormFile audioFile, IFormFile coverFile)
        {
            var results = new List<string>();
            var errors = new List<string>();

            try
            {
                // Upload audio file
                if (audioFile != null && audioFile.Length > 0)
                {
                    var audioResult = await _fileUploadService.UploadSongAudioAsync(songId, audioFile);
                    if (!string.IsNullOrEmpty(audioResult))
                    {
                        results.Add("Audio file uploaded successfully");
                    }
                    else
                    {
                        errors.Add("Failed to upload audio file");
                    }
                }

                // Upload cover file
                if (coverFile != null && coverFile.Length > 0)
                {
                    var coverResult = await _fileUploadService.UploadSongCoverAsync(songId, coverFile);
                    if (!string.IsNullOrEmpty(coverResult))
                    {
                        results.Add("Cover image uploaded successfully");
                    }
                    else
                    {
                        errors.Add("Failed to upload cover image");
                    }
                }

                if (results.Any())
                {
                    TempData["SuccessMessage"] = string.Join("; ", results);
                }
                if (errors.Any())
                {
                    TempData["ErrorMessage"] = string.Join("; ", errors);
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error uploading files: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // POST: AdminFileUpload/UploadAlbumCover
        [HttpPost]
        public async Task<IActionResult> UploadAlbumCover(int albumId, IFormFile coverFile)
        {
            try
            {
                if (coverFile == null || coverFile.Length == 0)
                {
                    TempData["ErrorMessage"] = "Please select a cover image.";
                    return RedirectToAction("Index");
                }

                var result = await _fileUploadService.UploadAlbumCoverAsync(albumId, coverFile);
                if (!string.IsNullOrEmpty(result))
                {
                    TempData["SuccessMessage"] = "Album cover uploaded successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to upload album cover.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error uploading album cover: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // POST: AdminFileUpload/UploadArtistImage
        [HttpPost]
        public async Task<IActionResult> UploadArtistImage(int artistId, IFormFile imageFile)
        {
            try
            {
                if (imageFile == null || imageFile.Length == 0)
                {
                    TempData["ErrorMessage"] = "Please select an image.";
                    return RedirectToAction("Index");
                }

                var result = await _fileUploadService.UploadArtistImageAsync(artistId, imageFile);
                if (!string.IsNullOrEmpty(result))
                {
                    TempData["SuccessMessage"] = "Artist image uploaded successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to upload artist image.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error uploading artist image: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // POST: AdminFileUpload/UploadPlaylistCover
        [HttpPost]
        public async Task<IActionResult> UploadPlaylistCover(int playlistId, IFormFile coverFile)
        {
            try
            {
                if (coverFile == null || coverFile.Length == 0)
                {
                    TempData["ErrorMessage"] = "Please select a cover image.";
                    return RedirectToAction("Index");
                }

                var result = await _fileUploadService.UploadPlaylistCoverAsync(playlistId, coverFile);
                if (!string.IsNullOrEmpty(result))
                {
                    TempData["SuccessMessage"] = "Playlist cover uploaded successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to upload playlist cover.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error uploading playlist cover: {ex.Message}";
            }

            return RedirectToAction("Index");
        }        // GET: AdminFileUpload/FileManager
        public async Task<IActionResult> FileManager()
        {
            try
            {
                // This would call the files API to list files
                // For now, just show the interface
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error loading file manager: {ex.Message}";
                return View();
            }
        }

        // POST: AdminFileUpload/Upload - Generic upload endpoint for the file manager
        [HttpPost]
        public async Task<IActionResult> Upload()
        {
            try
            {
                var fileType = Request.Form["fileType"].ToString();
                var fileName = Request.Form["fileName"].ToString();
                var entityName = Request.Form["entityName"].ToString();
                var imageFile = Request.Form.Files["imageFile"];
                var songFile = Request.Form.Files["songFile"];

                if (string.IsNullOrEmpty(fileType) || string.IsNullOrEmpty(fileName))
                {
                    return Json(new { success = false, message = "File type and name are required" });
                }

                var results = new List<string>();

                // Handle song files (both MP3 and cover)
                if (fileType == "songs")
                {
                    if (songFile != null && songFile.Length > 0)
                    {
                        var audioResult = await _fileUploadService.UploadSongAudioAsync(songFile, fileName, entityName);
                        if (audioResult.Success)
                            results.Add($"Audio uploaded: {audioResult.Message}");
                        else
                            return Json(new { success = false, message = $"Audio upload failed: {audioResult.Message}" });
                    }

                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var coverResult = await _fileUploadService.UploadSongCoverAsync(imageFile, fileName, entityName);
                        if (coverResult.Success)
                            results.Add($"Cover uploaded: {coverResult.Message}");
                        else
                            return Json(new { success = false, message = $"Cover upload failed: {coverResult.Message}" });
                    }
                }
                else if (imageFile != null && imageFile.Length > 0)
                {
                    // Handle other file types (albums, artists, users, playlists)
                    var result = fileType switch
                    {
                        "albums" => await _fileUploadService.UploadAlbumCoverAsync(imageFile, fileName, entityName),
                        "artists" => await _fileUploadService.UploadArtistImageAsync(imageFile, fileName, entityName),
                        "users" => await _fileUploadService.UploadUserAvatarAsync(imageFile, fileName, entityName),
                        "playlists" => await _fileUploadService.UploadPlaylistCoverAsync(imageFile, fileName, entityName),
                        _ => new UploadResult { Success = false, Message = "Invalid file type" }
                    };

                    if (result.Success)
                        results.Add(result.Message);
                    else
                        return Json(new { success = false, message = result.Message });
                }

                if (results.Count == 0)
                {
                    return Json(new { success = false, message = "No files were uploaded" });
                }

                return Json(new { success = true, message = string.Join(", ", results) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Upload error: {ex.Message}" });
            }
        }

        // POST: AdminFileUpload/Delete - Delete file endpoint
        [HttpPost]
        public async Task<IActionResult> Delete([FromBody] DeleteFileRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.FileType) || string.IsNullOrEmpty(request.FileName))
                {
                    return Json(new { success = false, message = "File type and name are required" });
                }

                var result = await _fileUploadService.DeleteFileAsync(request.FileType, request.FileName);
                return Json(new { success = result.Success, message = result.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Delete error: {ex.Message}" });
            }
        }

        // POST: AdminFileUpload/Cleanup - Cleanup unused files
        [HttpPost]
        public async Task<IActionResult> Cleanup()
        {
            try
            {
                var result = await _fileUploadService.CleanupUnusedFilesAsync();
                return Json(new { success = result.Success, message = result.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Cleanup error: {ex.Message}" });
            }
        }

        // POST: AdminFileUpload/GenerateThumbnails - Generate thumbnails for images
        [HttpPost]
        public async Task<IActionResult> GenerateThumbnails()
        {
            try
            {
                var result = await _fileUploadService.GenerateThumbnailsAsync();
                return Json(new { success = result.Success, message = result.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Thumbnail generation error: {ex.Message}" });
            }
        }
    }

    // Helper classes for API requests
    public class DeleteFileRequest
    {
        public string FileType { get; set; }
        public string FileName { get; set; }
    }
}

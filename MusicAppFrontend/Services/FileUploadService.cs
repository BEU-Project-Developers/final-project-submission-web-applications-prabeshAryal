using System.Text.Json;

namespace MusicApp.Services
{
    public class FileUploadService
    {
        private readonly ApiService _apiService;
        
        public FileUploadService(ApiService apiService)
        {
            _apiService = apiService;
        }
        
        public async Task<string> UploadProfileImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return null;
                
            return await _apiService.UploadFileAsync("api/Users/profile-image", file);
        }
        
        public async Task<string> UploadArtistImageAsync(int artistId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return null;
                
            return await _apiService.UploadFileAsync($"api/Artists/{artistId}/image", file);
        }
        
        public async Task<string> UploadAlbumCoverAsync(int albumId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return null;
                
            return await _apiService.UploadFileAsync($"api/Albums/{albumId}/cover", file);
        }
        
        public async Task<string> UploadSongCoverAsync(int songId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return null;
                
            return await _apiService.UploadFileAsync($"api/Songs/{songId}/cover", file);
        }
        
        public async Task<string> UploadSongAudioAsync(int songId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return null;
                
            return await _apiService.UploadFileAsync($"api/Songs/{songId}/audio", file);
        }
        
        public async Task<string> UploadPlaylistCoverAsync(int playlistId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return null;
                
            return await _apiService.UploadFileAsync($"api/Playlists/{playlistId}/cover", file);
        }

        // New methods for the admin file manager
        public async Task<UploadResult> UploadSongAudioAsync(IFormFile file, string fileName, string entityName = null)
        {
            if (file == null || file.Length == 0)
                return new UploadResult { Success = false, Message = "No file provided" };

            try
            {
                // Use the backend file storage API directly
                var result = await _apiService.UploadFileToStorageAsync("songs", file, fileName, entityName);
                return new UploadResult { Success = true, Message = result ?? "Audio file uploaded successfully" };
            }
            catch (Exception ex)
            {
                return new UploadResult { Success = false, Message = $"Audio upload failed: {ex.Message}" };
            }
        }

        public async Task<UploadResult> UploadSongCoverAsync(IFormFile file, string fileName, string entityName = null)
        {
            if (file == null || file.Length == 0)
                return new UploadResult { Success = false, Message = "No file provided" };

            try
            {
                var result = await _apiService.UploadFileToStorageAsync("songs", file, fileName, entityName);
                return new UploadResult { Success = true, Message = result ?? "Cover image uploaded successfully" };
            }
            catch (Exception ex)
            {
                return new UploadResult { Success = false, Message = $"Cover upload failed: {ex.Message}" };
            }
        }

        public async Task<UploadResult> UploadAlbumCoverAsync(IFormFile file, string fileName, string entityName = null)
        {
            if (file == null || file.Length == 0)
                return new UploadResult { Success = false, Message = "No file provided" };

            try
            {
                var result = await _apiService.UploadFileToStorageAsync("albums", file, fileName, entityName);
                return new UploadResult { Success = true, Message = result ?? "Album cover uploaded successfully" };
            }
            catch (Exception ex)
            {
                return new UploadResult { Success = false, Message = $"Album cover upload failed: {ex.Message}" };
            }
        }

        public async Task<UploadResult> UploadArtistImageAsync(IFormFile file, string fileName, string entityName = null)
        {
            if (file == null || file.Length == 0)
                return new UploadResult { Success = false, Message = "No file provided" };

            try
            {
                var result = await _apiService.UploadFileToStorageAsync("artists", file, fileName, entityName);
                return new UploadResult { Success = true, Message = result ?? "Artist image uploaded successfully" };
            }
            catch (Exception ex)
            {
                return new UploadResult { Success = false, Message = $"Artist image upload failed: {ex.Message}" };
            }
        }

        public async Task<UploadResult> UploadUserAvatarAsync(IFormFile file, string fileName, string entityName = null)
        {
            if (file == null || file.Length == 0)
                return new UploadResult { Success = false, Message = "No file provided" };

            try
            {
                var result = await _apiService.UploadFileToStorageAsync("users", file, fileName, entityName);
                return new UploadResult { Success = true, Message = result ?? "User avatar uploaded successfully" };
            }
            catch (Exception ex)
            {
                return new UploadResult { Success = false, Message = $"User avatar upload failed: {ex.Message}" };
            }
        }

        public async Task<UploadResult> UploadPlaylistCoverAsync(IFormFile file, string fileName, string entityName = null)
        {
            if (file == null || file.Length == 0)
                return new UploadResult { Success = false, Message = "No file provided" };

            try
            {
                var result = await _apiService.UploadFileToStorageAsync("playlists", file, fileName, entityName);
                return new UploadResult { Success = true, Message = result ?? "Playlist cover uploaded successfully" };
            }
            catch (Exception ex)
            {
                return new UploadResult { Success = false, Message = $"Playlist cover upload failed: {ex.Message}" };
            }
        }

        public async Task<UploadResult> DeleteFileAsync(string fileType, string fileName)
        {
            try
            {
                var result = await _apiService.DeleteFileFromStorageAsync(fileType, fileName);
                return new UploadResult { Success = true, Message = result ?? "File deleted successfully" };
            }
            catch (Exception ex)
            {
                return new UploadResult { Success = false, Message = $"Delete failed: {ex.Message}" };
            }
        }

        public async Task<UploadResult> CleanupUnusedFilesAsync()
        {
            try
            {
                var result = await _apiService.CleanupUnusedFilesAsync();
                return new UploadResult { Success = true, Message = result ?? "Cleanup completed successfully" };
            }
            catch (Exception ex)
            {
                return new UploadResult { Success = false, Message = $"Cleanup failed: {ex.Message}" };
            }
        }

        public async Task<UploadResult> GenerateThumbnailsAsync()
        {
            try
            {
                var result = await _apiService.GenerateThumbnailsAsync();
                return new UploadResult { Success = true, Message = result ?? "Thumbnails generated successfully" };
            }
            catch (Exception ex)
            {
                return new UploadResult { Success = false, Message = $"Thumbnail generation failed: {ex.Message}" };
            }
        }
    }

    public class UploadResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
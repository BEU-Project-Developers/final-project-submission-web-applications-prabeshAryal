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
    }
} 
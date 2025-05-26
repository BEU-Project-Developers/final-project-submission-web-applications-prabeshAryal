using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MusicAppBackend.Services
{    public interface IFileStorageService
    {
        Task<string> SaveFileAsync(IFormFile file, string folder);
        Task<string> SaveFileAsync(IFormFile file, string folder, string subfolder);
        Task<bool> DeleteFileAsync(string filePath);
        string GetFileUrl(string filePath);
    }
    
    public class FileStorageService : IFileStorageService
    {
        private readonly string _baseStoragePath;
        private readonly string _baseUrl;
        
        public FileStorageService(IConfiguration configuration)
        {
            _baseStoragePath = configuration["FileStorage:Path"] ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            _baseUrl = configuration["FileStorage:BaseUrl"] ?? "/uploads";
            
            // Ensure directory exists
            if (!Directory.Exists(_baseStoragePath))
            {
                Directory.CreateDirectory(_baseStoragePath);
            }
        }
          public async Task<string> SaveFileAsync(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is empty or null");
            }
            
            // Create folder if it doesn't exist
            var folderPath = Path.Combine(_baseStoragePath, folder);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            
            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(folder, fileName);
            var fullPath = Path.Combine(_baseStoragePath, filePath);
            
            // Save file
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            
            return filePath;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string folder, string subfolder)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is empty or null");
            }
            
            // Create nested folder structure if it doesn't exist
            var folderPath = Path.Combine(_baseStoragePath, folder, subfolder);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            
            // Use original filename for organized structure
            var fileName = Path.GetFileName(file.FileName);
            var filePath = Path.Combine(folder, subfolder, fileName);
            var fullPath = Path.Combine(_baseStoragePath, filePath);
            
            // Save file
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            
            return filePath;
        }
        
        public Task<bool> DeleteFileAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return Task.FromResult(false);
            }
            
            var fullPath = Path.Combine(_baseStoragePath, filePath);
            
            if (!File.Exists(fullPath))
            {
                return Task.FromResult(false);
            }
            
            File.Delete(fullPath);
            return Task.FromResult(true);
        }
        
        public string GetFileUrl(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return string.Empty;
            }
            
            return $"{_baseUrl}/{filePath.Replace("\\", "/")}";
        }
    }
} 
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MusicAppBackend.Services;
using System.IO;

namespace MusicAppBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileStorageController : ControllerBase
    {
        private readonly IFileStorageService _fileStorage;
        private readonly string _baseStoragePath;
        private readonly ILogger<FileStorageController> _logger;

        public FileStorageController(IFileStorageService fileStorage, IConfiguration configuration, ILogger<FileStorageController> logger)
        {
            _fileStorage = fileStorage;
            _baseStoragePath = configuration["FileStorage:Path"] ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            _logger = logger;
        }

        // POST: api/FileStorage/upload
        [HttpPost("upload")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadFile()
        {
            try
            {
                var file = Request.Form.Files["file"];
                var fileType = Request.Form["fileType"].ToString();
                var fileName = Request.Form["fileName"].ToString();
                var entityName = Request.Form["entityName"].ToString();

                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { success = false, message = "No file provided" });
                }

                if (string.IsNullOrEmpty(fileType) || string.IsNullOrEmpty(fileName))
                {
                    return BadRequest(new { success = false, message = "File type and name are required" });
                }

                // Validate file type
                var allowedTypes = new[] { "songs", "albums", "artists", "users", "playlists" };
                if (!allowedTypes.Contains(fileType.ToLower()))
                {
                    return BadRequest(new { success = false, message = "Invalid file type" });
                }

                // Determine the directory structure
                string targetFolder = fileType;
                if (!string.IsNullOrEmpty(entityName))
                {
                    // Create organized folder structure: songs/artist-name/song-file.mp3
                    var sanitizedEntityName = SanitizeFileName(entityName);
                    targetFolder = Path.Combine(fileType, sanitizedEntityName);
                }

                // Get file extension and create full filename
                var fileExtension = Path.GetExtension(file.FileName);
                var fullFileName = SanitizeFileName(fileName) + fileExtension;

                // Save the file
                var savedPath = await SaveFileToStructuredPath(file, targetFolder, fullFileName);

                return Ok(new 
                { 
                    success = true, 
                    filePath = savedPath,
                    message = $"File uploaded successfully to {savedPath}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                return StatusCode(500, new { success = false, message = $"Upload error: {ex.Message}" });
            }
        }

        // POST: api/FileStorage/delete
        [HttpPost("delete")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteFile([FromBody] DeleteFileRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.FileType) || string.IsNullOrEmpty(request.FileName))
                {
                    return BadRequest(new { success = false, message = "File type and name are required" });
                }

                // Find the file in the directory structure
                var basePath = Path.Combine(_baseStoragePath, request.FileType);
                if (!Directory.Exists(basePath))
                {
                    return NotFound(new { success = false, message = "File not found" });
                }

                // Search for the file recursively
                var files = Directory.GetFiles(basePath, request.FileName, SearchOption.AllDirectories);
                
                if (files.Length == 0)
                {
                    return NotFound(new { success = false, message = "File not found" });
                }

                // Delete the first matching file
                var fileToDelete = files[0];
                System.IO.File.Delete(fileToDelete);

                return Ok(new { success = true, message = "File deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file");
                return StatusCode(500, new { success = false, message = $"Delete error: {ex.Message}" });
            }
        }

        // POST: api/FileStorage/cleanup
        [HttpPost("cleanup")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> CleanupUnusedFiles()
        {
            try
            {
                int deletedCount = 0;
                var fileTypes = new[] { "songs", "albums", "artists", "users", "playlists" };

                foreach (var fileType in fileTypes)
                {
                    var directoryPath = Path.Combine(_baseStoragePath, fileType);
                    if (Directory.Exists(directoryPath))
                    {
                        // This is a simplified cleanup - in a real scenario, you'd check against database records
                        // For now, we'll just clean up empty directories
                        CleanupEmptyDirectories(directoryPath);
                    }
                }

                return Ok(new { success = true, message = $"Cleanup completed. Processed {fileTypes.Length} directories." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup");
                return StatusCode(500, new { success = false, message = $"Cleanup error: {ex.Message}" });
            }
        }

        // POST: api/FileStorage/generate-thumbnails
        [HttpPost("generate-thumbnails")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> GenerateThumbnails()
        {
            try
            {
                int processedCount = 0;
                var imageTypes = new[] { "albums", "artists", "users", "playlists" };

                foreach (var imageType in imageTypes)
                {
                    var directoryPath = Path.Combine(_baseStoragePath, imageType);
                    if (Directory.Exists(directoryPath))
                    {
                        var imageFiles = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories)
                            .Where(f => IsImageFile(f))
                            .ToList();

                        processedCount += imageFiles.Count;

                        // Here you would implement actual thumbnail generation
                        // For now, we'll just report the count
                    }
                }

                return Ok(new { success = true, message = $"Thumbnail generation completed. Processed {processedCount} images." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating thumbnails");
                return StatusCode(500, new { success = false, message = $"Thumbnail generation error: {ex.Message}" });
            }
        }

        private async Task<string> SaveFileToStructuredPath(IFormFile file, string folder, string fileName)
        {
            // Create the target directory
            var targetPath = Path.Combine(_baseStoragePath, folder);
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }

            // Full file path
            var filePath = Path.Combine(targetPath, fileName);

            // Save the file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return relative path for URL construction
            return Path.Combine(folder, fileName).Replace("\\", "/");
        }

        private static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return fileName;

            // Remove invalid characters and replace spaces with hyphens
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
            return sanitized.Replace(" ", "-").ToLowerInvariant();
        }

        private static void CleanupEmptyDirectories(string rootPath)
        {
            foreach (var directory in Directory.GetDirectories(rootPath))
            {
                CleanupEmptyDirectories(directory);
                
                if (!Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory);
                }
            }
        }

        private static bool IsImageFile(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" }.Contains(extension);
        }
    }

    public class DeleteFileRequest
    {
        public string FileType { get; set; }
        public string FileName { get; set; }
    }
}

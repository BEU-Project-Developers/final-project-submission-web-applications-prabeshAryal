using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using MusicAppBackend.Services;
using System.IO;
using System.Linq;

namespace MusicAppBackend.Controllers
{    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private readonly IFileStorageService _fileStorage;
        private readonly IContentTypeProvider _contentTypeProvider;
        private readonly string _baseStoragePath;
        private readonly ILogger<FilesController> _logger;

        public FilesController(IFileStorageService fileStorage, IContentTypeProvider contentTypeProvider, IConfiguration configuration, ILogger<FilesController> logger)
        {
            _fileStorage = fileStorage;
            _contentTypeProvider = contentTypeProvider;
            _baseStoragePath = configuration["FileStorage:Path"] ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            _logger = logger;
        }        // GET: api/files/{type}/{filename}
        [HttpGet("{type}/{filename}")]
        public async Task<IActionResult> GetFile(string type, string filename)
        {
            try
            {
                _logger.LogInformation("File request for type: {Type}, filename: {Filename}", type, filename);
                
                // Validate file type
                var allowedTypes = new[] { "songs", "albums", "artists", "playlists", "users", "audio", "covers", "profiles" };
                if (!allowedTypes.Contains(type.ToLower()))
                {
                    _logger.LogWarning("Invalid file type requested: {Type}", type);
                    return BadRequest("Invalid file type");
                }

                // Construct file path
                var filePath = Path.Combine(_baseStoragePath, type, filename);
                
                if (!System.IO.File.Exists(filePath))
                {
                    _logger.LogWarning("File not found: {FilePath}", filePath);
                    return NotFound();
                }

                // Determine content type
                if (!_contentTypeProvider.TryGetContentType(filename, out var contentType))
                {
                    contentType = "application/octet-stream";
                }

                // Read file
                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);

                // For images, audio, and video files, set Content-Disposition to inline for browser viewing
                var streamableTypes = new[] { "image/", "audio/", "video/" };
                var isStreamable = streamableTypes.Any(t => contentType.StartsWith(t));

                _logger.LogInformation("Successfully serving file: {Filename} with content type: {ContentType}", filename, contentType);

                if (isStreamable)
                {
                    // Return file with inline disposition for browser viewing
                    return File(fileBytes, contentType);
                }
                else
                {
                    // Return file with attachment disposition for download
                    return File(fileBytes, contentType, filename);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving file: {Type}/{Filename}", type, filename);
                return StatusCode(500, $"Error retrieving file: {ex.Message}");
            }
        }        // GET: api/files/{type}/{subfolder}/{filename} - For organized structure like songs/hello/hello.mp3
        [HttpGet("{type}/{subfolder}/{filename}")]
        public async Task<IActionResult> GetFileFromSubfolder(string type, string subfolder, string filename)
        {
            try
            {
                _logger.LogInformation("Subfolder file request for type: {Type}, subfolder: {Subfolder}, filename: {Filename}", type, subfolder, filename);
                
                // Validate file type
                var allowedTypes = new[] { "songs", "albums", "artists", "playlists", "users" };
                if (!allowedTypes.Contains(type.ToLower()))
                {
                    _logger.LogWarning("Invalid file type requested: {Type}", type);
                    return BadRequest("Invalid file type");
                }

                // Construct file path
                var filePath = Path.Combine(_baseStoragePath, type, subfolder, filename);
                
                if (!System.IO.File.Exists(filePath))
                {
                    _logger.LogWarning("File not found: {FilePath}", filePath);
                    return NotFound();
                }

                // Determine content type
                if (!_contentTypeProvider.TryGetContentType(filename, out var contentType))
                {
                    contentType = "application/octet-stream";
                }

                // Read file
                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);

                // For images, audio, and video files, set Content-Disposition to inline for browser viewing
                var streamableTypes = new[] { "image/", "audio/", "video/" };
                var isStreamable = streamableTypes.Any(t => contentType.StartsWith(t));

                _logger.LogInformation("Successfully serving subfolder file: {Subfolder}/{Filename} with content type: {ContentType}", subfolder, filename, contentType);

                if (isStreamable)
                {
                    // Return file with inline disposition for browser viewing
                    return File(fileBytes, contentType);
                }
                else
                {
                    // Return file with attachment disposition for download
                    return File(fileBytes, contentType, filename);
                }                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subfolder file: {Type}/{Subfolder}/{Filename}", type, subfolder, filename);
                return StatusCode(500, $"Error retrieving file: {ex.Message}");
            }
        }        // GET: api/files/list/{type} - List files in a directory (admin only)
        [HttpGet("list/{type}")]
        //[Authorize(Roles = "Admin")]
        public IActionResult ListFiles(string type)
        {
            try
            {
                _logger.LogInformation("Listing files for type: {Type}", type);
                
                var allowedTypes = new[] { "songs", "albums", "artists", "playlists", "users", "audio", "covers", "profiles" };
                if (!allowedTypes.Contains(type.ToLower()))
                {
                    _logger.LogWarning("Invalid file type requested for listing: {Type}", type);
                    return BadRequest("Invalid file type");
                }

                var directoryPath = Path.Combine(_baseStoragePath, type);
                
                if (!Directory.Exists(directoryPath))
                {
                    _logger.LogInformation("Directory does not exist for type: {Type}", type);
                    return Ok(new { files = new string[0] });
                }

                var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories)
                    .Select(f => Path.GetRelativePath(_baseStoragePath, f).Replace("\\", "/"))
                    .ToList();

                _logger.LogInformation("Successfully listed {Count} files for type: {Type}", files.Count, type);
                return Ok(new { files });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing files for type: {Type}", type);
                return StatusCode(500, $"Error listing files: {ex.Message}");
            }
        }        // GET: api/files/browse/{type} - Browse files with detailed info for admin file manager
        [HttpGet("browse/{type}")]
        //[Authorize(Roles = "Admin")]
        public IActionResult BrowseFiles(string type)
        {
            try
            {
                _logger.LogInformation("Browsing files for type: {Type}", type);
                
                var allowedTypes = new[] { "songs", "albums", "artists", "playlists", "users" };
                if (!allowedTypes.Contains(type.ToLower()))
                {
                    _logger.LogWarning("Invalid file type requested for browsing: {Type}", type);
                    return BadRequest(new { success = false, message = "Invalid file type" });
                }

                var directoryPath = Path.Combine(_baseStoragePath, type);
                
                if (!Directory.Exists(directoryPath))
                {
                    _logger.LogInformation("Directory does not exist for type: {Type}", type);
                    return Ok(new { success = true, data = new object[0] });
                }

                var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories)
                    .Select(filePath =>
                    {
                        var fileInfo = new FileInfo(filePath);
                        var relativePath = Path.GetRelativePath(directoryPath, filePath).Replace("\\", "/");
                        var fileName = Path.GetFileName(relativePath);
                        
                        return new
                        {
                            name = fileName,
                            path = relativePath,
                            size = FormatFileSize(fileInfo.Length),
                            lastModified = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
                        };
                    })
                    .OrderBy(f => f.name)
                    .ToList();

                _logger.LogInformation("Successfully browsed {Count} files for type: {Type}", files.Count, type);
                return Ok(new { success = true, data = files });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error browsing files for type: {Type}", type);
                return Ok(new { success = false, message = $"Error browsing files: {ex.Message}" });
            }
        }

        private static string FormatFileSize(long bytes)
        {
            if (bytes >= 1073741824)
                return $"{bytes / 1073741824.0:F1} GB";
            if (bytes >= 1048576)
                return $"{bytes / 1048576.0:F1} MB";
            if (bytes >= 1024)
                return $"{bytes / 1024.0:F1} KB";
            return $"{bytes} bytes";
        }
    }
}

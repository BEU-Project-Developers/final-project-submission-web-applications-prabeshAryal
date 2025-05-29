using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MusicAppBackend.Data;
using MusicAppBackend.Models;
using MusicAppBackend.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MusicAppBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlbumsController : BaseController
    {
        private readonly IFileStorageService _fileStorage;

        public AlbumsController(MusicDbContext context, IFileStorageService fileStorage, ILogger<AlbumsController> logger)
            : base(context, logger)
        {
            _fileStorage = fileStorage;
        }

        // GET: api/Albums
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetAlbums(
            [FromQuery] int? artistId,
            [FromQuery] string? genre,
            [FromQuery] int? year,
            [FromQuery] string? sortBy,
            [FromQuery] bool desc = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            IQueryable<Album> query = _context.Albums
                .Include(a => a.Artist);

            // Apply filters
            if (artistId.HasValue)
            {
                query = query.Where(a => a.ArtistId == artistId);
            }

            if (!string.IsNullOrEmpty(genre))
            {
                query = query.Where(a => a.Genre == genre);
            }

            if (year.HasValue)
            {
                query = query.Where(a => a.Year == year);
            }

            // Apply sorting
            if (!string.IsNullOrEmpty(sortBy))
            {
                switch (sortBy.ToLower())
                {
                    case "title":
                        query = desc ? query.OrderByDescending(a => a.Title) : query.OrderBy(a => a.Title);
                        break;
                    case "artist":
                        query = desc ? query.OrderByDescending(a => a.Artist.Name) : query.OrderBy(a => a.Artist.Name);
                        break;
                    case "year":
                        query = desc ? query.OrderByDescending(a => a.Year) : query.OrderBy(a => a.Year);
                        break;
                    case "releasedate":
                        query = desc ? query.OrderByDescending(a => a.ReleaseDate) : query.OrderBy(a => a.ReleaseDate);
                        break;
                    default:
                        query = desc ? query.OrderByDescending(a => a.Id) : query.OrderBy(a => a.Id);
                        break;
                }
            }
            else
            {
                // Default sort
                query = query.OrderByDescending(a => a.ReleaseDate ?? DateTime.MinValue);
            }

            // Apply pagination
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var albums = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.ArtistId,
                    ArtistName = a.Artist.Name,
                    CoverImageUrl = !string.IsNullOrEmpty(a.CoverImageUrl) ? 
                        _fileStorage.GetFileUrl(a.CoverImageUrl) : null,
                    a.Year,
                    a.Genre,
                    a.ReleaseDate,
                    a.TotalTracks,
                    a.Duration
                })
                .ToListAsync();

            return Ok(new
            {
                TotalCount = totalCount,
                TotalPages = totalPages,
                CurrentPage = page,
                PageSize = pageSize,
                Data = albums
            });
        }

    // GET: api/Albums/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetAlbum(int id)
        {
            var album = await _context.Albums
                .Include(a => a.Artist)
                .Include(a => a.Songs)
                    .ThenInclude(s => s.Artist) // Ensure Artist is loaded for each song
                .FirstOrDefaultAsync(a => a.Id == id);

            if (album == null)
            {
                return NotFound();
            }

            // Check if album is liked by current user (if authenticated)
            bool isLiked = false;
            if (User.Identity?.IsAuthenticated ?? false)
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                isLiked = await _context.AlbumLikes.AnyAsync(al => al.AlbumId == id && al.UserId == userId);
            }

            return new
            {
                album.Id,
                album.Title,
                album.ArtistId,
                ArtistName = album.Artist.Name, // Artist of the album
                CoverImageUrl = !string.IsNullOrEmpty(album.CoverImageUrl) ? 
                    _fileStorage.GetFileUrl(album.CoverImageUrl) : null,
                album.Year,
                album.Description,
                album.Genre,
                album.ReleaseDate,
                album.TotalTracks,
                album.Duration,
                IsLiked = isLiked, // Add like status in the response
                Songs = album.Songs.Select(s => new
                {
                    s.Id,
                    s.Title,
                    ArtistName = s.Artist != null ? s.Artist.Name : "Unknown Artist", // Artist of the song
                    s.TrackNumber,
                    s.Duration,
                    AudioUrl = !string.IsNullOrEmpty(s.AudioUrl) ? 
                        _fileStorage.GetFileUrl(s.AudioUrl) : null,
                    CoverImageUrl = !string.IsNullOrEmpty(s.CoverImageUrl) ? 
                        _fileStorage.GetFileUrl(s.CoverImageUrl) : 
                        (!string.IsNullOrEmpty(album.CoverImageUrl) ? 
                            _fileStorage.GetFileUrl(album.CoverImageUrl) : null),
                    s.PlayCount
                }).OrderBy(s => s.TrackNumber).ToList()
            };
        }

        // POST: api/Albums
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Album>> CreateAlbum([FromBody] AlbumCreateDTO albumDto)
        {
            var artist = await _context.Artists.FindAsync(albumDto.ArtistId);
            if (artist == null)
            {
                return BadRequest("Artist not found");
            }

            var album = new Album
            {
                Title = albumDto.Title,
                ArtistId = albumDto.ArtistId,
                Year = albumDto.Year,
                Description = albumDto.Description,
                Genre = albumDto.Genre,
                ReleaseDate = albumDto.ReleaseDate,
                TotalTracks = albumDto.TotalTracks,
                Duration = albumDto.Duration,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };            _context.Albums.Add(album);
            await _context.SaveChangesAsync();

            // Return a simple DTO to avoid circular references
            return CreatedAtAction(nameof(GetAlbum), new { id = album.Id }, new
            {
                album.Id,
                album.Title,
                album.ArtistId,
                album.Year,
                album.Description,
                album.Genre,
                album.ReleaseDate,
                album.TotalTracks,
                album.Duration,
                album.CreatedAt,
                album.UpdatedAt
            });
        }

        // PUT: api/Albums/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateAlbum(int id, [FromBody] AlbumUpdateDTO albumDto)
        {
            var album = await _context.Albums.FindAsync(id);
            if (album == null)
            {
                return NotFound();
            }

            if (albumDto.ArtistId.HasValue)
            {
                var artist = await _context.Artists.FindAsync(albumDto.ArtistId.Value);
                if (artist == null)
                {
                    return BadRequest("Artist not found");
                }
                album.ArtistId = albumDto.ArtistId.Value;
            }

            album.Title = albumDto.Title ?? album.Title;
            album.Year = albumDto.Year ?? album.Year;
            album.Description = albumDto.Description ?? album.Description;
            album.Genre = albumDto.Genre ?? album.Genre;
            album.ReleaseDate = albumDto.ReleaseDate ?? album.ReleaseDate;
            album.TotalTracks = albumDto.TotalTracks ?? album.TotalTracks;
            album.Duration = albumDto.Duration ?? album.Duration;
            album.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await AlbumExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Albums/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAlbum(int id)
        {
            var album = await _context.Albums
                .Include(a => a.Songs)
                .FirstOrDefaultAsync(a => a.Id == id);
                
            if (album == null)
            {
                return NotFound();
            }

            // Delete album cover if exists
            if (!string.IsNullOrEmpty(album.CoverImageUrl))
            {
                await _fileStorage.DeleteFileAsync(album.CoverImageUrl);
            }

            // Remove the album
            _context.Albums.Remove(album);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/Albums/5/cover
        [HttpPost("{id}/cover")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<string>> UploadCoverImage(int id, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            var album = await _context.Albums.FindAsync(id);
            if (album == null)
            {
                return NotFound();
            }

            // Delete old cover if exists
            if (!string.IsNullOrEmpty(album.CoverImageUrl))
            {
                await _fileStorage.DeleteFileAsync(album.CoverImageUrl);
            }

            // Save new cover
            var filePath = await _fileStorage.SaveFileAsync(file, "albums");
            album.CoverImageUrl = filePath;
            album.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { coverImageUrl = _fileStorage.GetFileUrl(filePath) });
        }        // POST: api/Albums/{id}/toggleLike
        [HttpPost("{id}/toggleLike")]
        [Authorize]
        public async Task<IActionResult> ToggleLike(int id)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var album = await _context.Albums.FindAsync(id);
            if (album == null)
            {
                return NotFound();
            }

            // Check if already liked
            var existingLike = await _context.AlbumLikes.FirstOrDefaultAsync(al => al.AlbumId == id && al.UserId == userId);
            
            if (existingLike != null)
            {
                // Unlike: Remove the existing like
                _context.AlbumLikes.Remove(existingLike);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Album unliked successfully.", isLiked = false });
            }
            else
            {
                // Like: Add new like
                _context.AlbumLikes.Add(new AlbumLike
                {
                    AlbumId = id,
                    UserId = userId,
                    LikedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
                return Ok(new { message = "Album liked successfully.", isLiked = true });
            }
        }        // (Removed: GET: api/Albums/{id}/liked)
        // The IsAlbumLiked endpoint is no longer needed with the new toggleLike and isLiked in album response.

        private async Task<bool> AlbumExists(int id)
        {
            return await _context.Albums.AnyAsync(a => a.Id == id);
        }
    }

    public class AlbumCreateDTO
    {
        public string Title { get; set; } = string.Empty;
        public int ArtistId { get; set; }
        public int? Year { get; set; }
        public string? Description { get; set; }
        public string? Genre { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public int? TotalTracks { get; set; }
        public TimeSpan? Duration { get; set; }
    }

    public class AlbumUpdateDTO
    {
        public string? Title { get; set; }
        public int? ArtistId { get; set; }
        public int? Year { get; set; }
        public string? Description { get; set; }
        public string? Genre { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public int? TotalTracks { get; set; }
        public TimeSpan? Duration { get; set; }
    }
}
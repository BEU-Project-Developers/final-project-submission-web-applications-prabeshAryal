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
using System.Security.Claims;
using System.Threading.Tasks;

namespace MusicAppBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SongsController : BaseController
    {
        private readonly IFileStorageService _fileStorage;

        public SongsController(MusicDbContext context, IFileStorageService fileStorage, ILogger<SongsController> logger)
            : base(context, logger)
        {
            _fileStorage = fileStorage;
        }

        // GET: api/Songs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetSongs(
            [FromQuery] int? artistId,
            [FromQuery] int? albumId,
            [FromQuery] string? genre,
            [FromQuery] string? search,
            [FromQuery] string? sortBy,
            [FromQuery] bool desc = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                IQueryable<Song> query = _context.Songs
                    .Include(s => s.Artist)
                    .Include(s => s.Album);

                // Apply filters
                if (artistId.HasValue)
                {
                    query = query.Where(s => s.ArtistId == artistId);
                }

                if (albumId.HasValue)
                {
                    query = query.Where(s => s.AlbumId == albumId);
                }

                if (!string.IsNullOrEmpty(genre))
                {
                    query = query.Where(s => s.Genre == genre);
                }

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(s => s.Title.Contains(search) || 
                                             (s.Artist != null && s.Artist.Name.Contains(search)) || 
                                             (s.Album != null && s.Album.Title.Contains(search)));
                }

                // Apply sorting
                if (!string.IsNullOrEmpty(sortBy))
                {
                    switch (sortBy.ToLower())
                    {
                        case "title":
                            query = desc ? query.OrderByDescending(s => s.Title) : query.OrderBy(s => s.Title);
                            break;
                        case "artist":
                            query = desc ? query.OrderByDescending(s => s.Artist != null ? s.Artist.Name : null) : query.OrderBy(s => s.Artist != null ? s.Artist.Name : null);
                            break;
                        case "album":
                            query = desc ? query.OrderByDescending(s => s.Album != null ? s.Album.Title : null) : query.OrderBy(s => s.Album != null ? s.Album.Title : null);
                            break;
                        case "duration":
                            query = desc ? query.OrderByDescending(s => s.Duration) : query.OrderBy(s => s.Duration);
                            break;
                        case "releasedate":
                            query = desc ? query.OrderByDescending(s => s.ReleaseDate) : query.OrderBy(s => s.ReleaseDate);
                            break;
                        case "playcount":
                            query = desc ? query.OrderByDescending(s => s.PlayCount) : query.OrderBy(s => s.PlayCount);
                            break;
                        default:
                            query = desc ? query.OrderByDescending(s => s.Id) : query.OrderBy(s => s.Id);
                            break;
                    }
                }
                else
                {
                    // Default sort
                    query = query.OrderByDescending(s => s.PlayCount);
                }

                // Apply pagination
                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                var songs = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => new
                    {
                        s.Id,
                        s.Title,
                        s.ArtistId,
                        ArtistName = s.Artist != null ? s.Artist.Name : null,
                        s.AlbumId,
                        AlbumTitle = s.Album != null ? s.Album.Title : null,
                        s.Duration,
                        AudioUrl = !string.IsNullOrEmpty(s.AudioUrl) ? 
                            _fileStorage.GetFileUrl(s.AudioUrl) : null,
                        CoverImageUrl = !string.IsNullOrEmpty(s.CoverImageUrl) ? 
                            _fileStorage.GetFileUrl(s.CoverImageUrl) : 
                            (s.Album != null && !string.IsNullOrEmpty(s.Album.CoverImageUrl) ? 
                                _fileStorage.GetFileUrl(s.Album.CoverImageUrl) : null),
                        s.TrackNumber,
                        s.Genre,
                        s.ReleaseDate,
                        s.PlayCount
                    })
                    .ToListAsync();

                return Ok(new
                {
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    CurrentPage = page,
                    PageSize = pageSize,
                    Data = songs
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving songs: {ex.Message}");
            }
        }

        // GET: api/Songs/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetSong(int id)
        {
            var song = await _context.Songs
                .Include(s => s.Artist)
                .Include(s => s.Album)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (song == null)
            {
                return NotFound();
            }

            return new
            {
                song.Id,
                song.Title,
                song.ArtistId,
                ArtistName = song.Artist?.Name,
                song.AlbumId,
                AlbumTitle = song.Album?.Title,
                song.Duration,
                AudioUrl = !string.IsNullOrEmpty(song.AudioUrl) ? 
                    _fileStorage.GetFileUrl(song.AudioUrl) : null,
                CoverImageUrl = !string.IsNullOrEmpty(song.CoverImageUrl) ? 
                    _fileStorage.GetFileUrl(song.CoverImageUrl) : 
                    (!string.IsNullOrEmpty(song.Album?.CoverImageUrl) ? 
                        _fileStorage.GetFileUrl(song.Album.CoverImageUrl) : null),
                song.TrackNumber,
                song.Genre,
                song.ReleaseDate,
                song.PlayCount            };
        }

        // GET: api/Songs/5/similar
        [HttpGet("{id}/similar")]
        public async Task<ActionResult<IEnumerable<object>>> GetSimilarSongs(int id, [FromQuery] int limit = 5)
        {
            try
            {
                // Get the reference song
                var referenceSong = await _context.Songs
                    .Include(s => s.Artist)
                    .Include(s => s.Album)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (referenceSong == null)
                {
                    return NotFound();
                }

                // Find similar songs based on genre and artist
                var similarSongs = await _context.Songs
                    .Include(s => s.Artist)
                    .Include(s => s.Album)
                    .Where(s => s.Id != id && // Exclude the reference song itself
                               (s.Genre == referenceSong.Genre || // Same genre
                                s.ArtistId == referenceSong.ArtistId)) // Same artist
                    .OrderBy(s => s.ArtistId == referenceSong.ArtistId ? 0 : 1) // Prioritize same artist
                    .ThenByDescending(s => s.PlayCount) // Then by popularity
                    .Take(limit)
                    .Select(s => new
                    {
                        s.Id,
                        s.Title,
                        s.ArtistId,
                        ArtistName = s.Artist != null ? s.Artist.Name : null,
                        s.AlbumId,
                        AlbumTitle = s.Album != null ? s.Album.Title : null,
                        s.Duration,
                        AudioUrl = !string.IsNullOrEmpty(s.AudioUrl) ? 
                            _fileStorage.GetFileUrl(s.AudioUrl) : null,
                        CoverImageUrl = !string.IsNullOrEmpty(s.CoverImageUrl) ? 
                            _fileStorage.GetFileUrl(s.CoverImageUrl) : 
                            (s.Album != null && !string.IsNullOrEmpty(s.Album.CoverImageUrl) ? 
                                _fileStorage.GetFileUrl(s.Album.CoverImageUrl) : null),
                        s.TrackNumber,
                        s.Genre,
                        s.ReleaseDate,
                        s.PlayCount
                    })
                    .ToListAsync();

                return Ok(similarSongs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving similar songs: {ex.Message}");
            }
        }

        // POST: api/Songs
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Song>> CreateSong([FromBody] SongCreateDTO songDto)
        {
            // Validate artist and album if provided
            if (songDto.ArtistId.HasValue)
            {
                var artist = await _context.Artists.FindAsync(songDto.ArtistId.Value);
                if (artist == null)
                {
                    return BadRequest("Artist not found");
                }
            }

            if (songDto.AlbumId.HasValue)
            {
                var album = await _context.Albums.FindAsync(songDto.AlbumId.Value);
                if (album == null)
                {
                    return BadRequest("Album not found");
                }
            }

            var song = new Song
            {
                Title = songDto.Title,
                ArtistId = songDto.ArtistId,
                AlbumId = songDto.AlbumId,
                Duration = songDto.Duration,
                TrackNumber = songDto.TrackNumber,
                Genre = songDto.Genre,
                ReleaseDate = songDto.ReleaseDate,
                PlayCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };            _context.Songs.Add(song);
            await _context.SaveChangesAsync();

            // Return a simple DTO to avoid circular references
            return CreatedAtAction(nameof(GetSong), new { id = song.Id }, new
            {
                song.Id,
                song.Title,
                song.ArtistId,
                song.AlbumId,
                song.Duration,
                song.TrackNumber,
                song.Genre,
                song.ReleaseDate,
                song.PlayCount,
                song.CreatedAt,
                song.UpdatedAt
            });
        }

        // PUT: api/Songs/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateSong(int id, [FromBody] SongUpdateDTO songDto)
        {
            var song = await _context.Songs.FindAsync(id);
            if (song == null)
            {
                return NotFound();
            }

            // Validate artist and album if provided
            if (songDto.ArtistId.HasValue)
            {
                var artist = await _context.Artists.FindAsync(songDto.ArtistId.Value);
                if (artist == null)
                {
                    return BadRequest("Artist not found");
                }
                song.ArtistId = songDto.ArtistId.Value;
            }

            if (songDto.AlbumId.HasValue)
            {
                var album = await _context.Albums.FindAsync(songDto.AlbumId.Value);
                if (album == null)
                {
                    return BadRequest("Album not found");
                }
                song.AlbumId = songDto.AlbumId.Value;
            }

            song.Title = songDto.Title ?? song.Title;
            song.Duration = songDto.Duration ?? song.Duration;
            song.AudioUrl = songDto.AudioUrl ?? song.AudioUrl;
            song.CoverImageUrl = songDto.CoverImageUrl ?? song.CoverImageUrl;
            song.TrackNumber = songDto.TrackNumber ?? song.TrackNumber;
            song.Genre = songDto.Genre ?? song.Genre;
            song.ReleaseDate = songDto.ReleaseDate ?? song.ReleaseDate;
            song.PlayCount = songDto.PlayCount ?? song.PlayCount;
            song.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await SongExists(id))
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

        // DELETE: api/Songs/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteSong(int id)
        {
            var song = await _context.Songs.FindAsync(id);
            if (song == null)
            {
                return NotFound();
            }

            // Delete audio file if exists
            if (!string.IsNullOrEmpty(song.AudioUrl))
            {
                await _fileStorage.DeleteFileAsync(song.AudioUrl);
            }

            // Delete cover image if exists
            if (!string.IsNullOrEmpty(song.CoverImageUrl))
            {
                await _fileStorage.DeleteFileAsync(song.CoverImageUrl);
            }

            _context.Songs.Remove(song);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/Songs/5/audio
        [HttpPost("{id}/audio")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<string>> UploadAudio(int id, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            var song = await _context.Songs.FindAsync(id);
            if (song == null)
            {
                return NotFound();
            }

            // Delete old audio file if exists
            if (!string.IsNullOrEmpty(song.AudioUrl))
            {
                await _fileStorage.DeleteFileAsync(song.AudioUrl);
            }

            // Save new audio file
            var filePath = await _fileStorage.SaveFileAsync(file, "audio");
            song.AudioUrl = filePath;
            song.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { audioUrl = _fileStorage.GetFileUrl(filePath) });
        }

        // POST: api/Songs/5/cover
        [HttpPost("{id}/cover")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<string>> UploadCover(int id, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            var song = await _context.Songs.FindAsync(id);
            if (song == null)
            {
                return NotFound();
            }

            // Delete old cover if exists
            if (!string.IsNullOrEmpty(song.CoverImageUrl))
            {
                await _fileStorage.DeleteFileAsync(song.CoverImageUrl);
            }

            // Save new cover
            var filePath = await _fileStorage.SaveFileAsync(file, "covers");
            song.CoverImageUrl = filePath;
            song.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { coverImageUrl = _fileStorage.GetFileUrl(filePath) });
        }

        // POST: api/Songs/5/play
        [HttpPost("{id}/play")]
        public async Task<IActionResult> PlaySong(int id)
        {
            var song = await _context.Songs.FindAsync(id);
            if (song == null)
            {
                return NotFound();
            }

            // Increment play count
            song.PlayCount += 1;
            await _context.SaveChangesAsync();

            return Ok(new { playCount = song.PlayCount });
        }

        // POST: api/Songs/5/favorite
        [HttpPost("{id}/favorite")]
        [Authorize]
        public async Task<IActionResult> FavoriteSong(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var song = await _context.Songs.FindAsync(id);
            if (song == null)
            {
                return NotFound();
            }

            var existingFavorite = await _context.UserFavorites
                .FirstOrDefaultAsync(uf => uf.UserId == userId && uf.SongId == id);

            if (existingFavorite != null)
            {
                return BadRequest("Song already in favorites");
            }

            _context.UserFavorites.Add(new UserFavorite
            {
                UserId = userId,
                SongId = id,
                AddedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok();
        }

        // DELETE: api/Songs/5/favorite
        [HttpDelete("{id}/favorite")]
        [Authorize]
        public async Task<IActionResult> UnfavoriteSong(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var favorite = await _context.UserFavorites
                .FirstOrDefaultAsync(uf => uf.UserId == userId && uf.SongId == id);

            if (favorite == null)
            {
                return NotFound("Song not in favorites");
            }

            _context.UserFavorites.Remove(favorite);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // GET: api/Songs/favorites
        [HttpGet("favorites")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<object>>> GetFavorites(
            [FromQuery] string? sortBy,
            [FromQuery] bool desc = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            IQueryable<UserFavorite> query = _context.UserFavorites
                .Where(uf => uf.UserId == userId)
                .Include(uf => uf.Song)
                .ThenInclude(s => s!.Artist)
                .Include(uf => uf.Song)
                .ThenInclude(s => s!.Album);

            // Apply sorting
            if (!string.IsNullOrEmpty(sortBy))
            {
                switch (sortBy.ToLower())
                {
                    case "title":
                        query = desc ? query.OrderByDescending(uf => uf.Song.Title) : query.OrderBy(uf => uf.Song.Title);
                        break;
                    case "artist":
                        query = desc ? query.OrderByDescending(uf => uf.Song.Artist!.Name) : query.OrderBy(uf => uf.Song.Artist!.Name);
                        break;
                    case "addedat":
                        query = desc ? query.OrderByDescending(uf => uf.AddedAt) : query.OrderBy(uf => uf.AddedAt);
                        break;
                    default:
                        query = desc ? query.OrderByDescending(uf => uf.AddedAt) : query.OrderBy(uf => uf.AddedAt);
                        break;
                }
            }
            else
            {
                // Default sort by date added descending
                query = query.OrderByDescending(uf => uf.AddedAt);
            }

            // Apply pagination
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var favorites = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(uf => new
                {
                    uf.Song.Id,
                    uf.Song.Title,
                    uf.Song.ArtistId,
                    ArtistName = uf.Song.Artist != null ? uf.Song.Artist.Name : null,
                    uf.Song.AlbumId,
                    AlbumTitle = uf.Song.Album != null ? uf.Song.Album.Title : null,
                    uf.Song.Duration,
                    AudioUrl = !string.IsNullOrEmpty(uf.Song.AudioUrl) ? 
                        _fileStorage.GetFileUrl(uf.Song.AudioUrl) : null,
                    CoverImageUrl = !string.IsNullOrEmpty(uf.Song.CoverImageUrl) ? 
                        _fileStorage.GetFileUrl(uf.Song.CoverImageUrl) : 
                        (!string.IsNullOrEmpty(uf.Song.Album!.CoverImageUrl) ? 
                            _fileStorage.GetFileUrl(uf.Song.Album.CoverImageUrl) : null),
                    uf.Song.Genre,
                    AddedAt = uf.AddedAt
                })
                .ToListAsync();

            return Ok(new
            {
                TotalCount = totalCount,
                TotalPages = totalPages,
                CurrentPage = page,
                PageSize = pageSize,
                Data = favorites
            });
        }

        // GET: api/Songs/genres
        [HttpGet("genres")]
        public async Task<ActionResult<IEnumerable<string>>> GetGenres()
        {
            var genres = await _context.Songs
                .Where(s => s.Genre != null)
                .Select(s => s.Genre)
                .Distinct()
                .OrderBy(g => g)
                .ToListAsync();

            return genres!;
        }

        private async Task<bool> SongExists(int id)
        {
            return await _context.Songs.AnyAsync(s => s.Id == id);
        }
    }

    public class SongCreateDTO
    {
        public string Title { get; set; } = string.Empty;
        public int? ArtistId { get; set; }
        public int? AlbumId { get; set; }
        public TimeSpan Duration { get; set; }
        public int? TrackNumber { get; set; }
        public string? Genre { get; set; }
        public DateTime? ReleaseDate { get; set; }
    }

    public class SongUpdateDTO
    {
        public string? Title { get; set; }
        public int? ArtistId { get; set; }
        public int? AlbumId { get; set; }
        public TimeSpan? Duration { get; set; }
        public string? AudioUrl { get; set; }
        public string? CoverImageUrl { get; set; }
        public int? TrackNumber { get; set; }
        public string? Genre { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public int? PlayCount { get; set; }
    }
}
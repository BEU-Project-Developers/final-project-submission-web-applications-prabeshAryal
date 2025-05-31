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
    public class PlaylistsController : BaseController
    {
        private readonly IFileStorageService _fileStorage;

        public PlaylistsController(MusicDbContext context, IFileStorageService fileStorage, ILogger<PlaylistsController> logger)
            : base(context, logger)
        {
            _fileStorage = fileStorage;
        }        // GET: api/Playlists
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetPlaylists(
            [FromQuery] string? search,
            [FromQuery] string? sortBy,
            [FromQuery] bool desc = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool adminViewAll = false)
        {
            // Get playlists based on user authentication and admin status
            IQueryable<Playlist> query = _context.Playlists
                .Include(p => p.User);

            if (!User.Identity!.IsAuthenticated)
            {
                query = query.Where(p => p.IsPublic);
            }
            else if (adminViewAll && User.IsInRole("Admin"))
            {
                // Admin viewing all playlists - no filtering
            }
            else
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                query = query.Where(p => p.IsPublic || p.UserId == userId);
            }

            // Apply search
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search) || 
                                         p.Description!.Contains(search) || 
                                         p.User.Username.Contains(search));
            }

            // Apply sorting
            if (!string.IsNullOrEmpty(sortBy))
            {
                switch (sortBy.ToLower())
                {
                    case "name":
                        query = desc ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name);
                        break;
                    case "user":
                        query = desc ? query.OrderByDescending(p => p.User.Username) : query.OrderBy(p => p.User.Username);
                        break;
                    case "createdat":
                        query = desc ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt);
                        break;
                    default:
                        query = desc ? query.OrderByDescending(p => p.Id) : query.OrderBy(p => p.Id);
                        break;
                }
            }
            else
            {
                // Default sort
                query = query.OrderByDescending(p => p.CreatedAt);
            }

            // Apply pagination
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var playlists = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    p.UserId,
                    CreatorUsername = p.User.Username,
                    CoverImageUrl = !string.IsNullOrEmpty(p.CoverImageUrl) ? 
                        _fileStorage.GetFileUrl(p.CoverImageUrl) : null,
                    p.IsPublic,
                    p.CreatedAt,
                    p.UpdatedAt,
                    SongCount = p.PlaylistSongs.Count
                })
                .ToListAsync();

            return Ok(new
            {
                TotalCount = totalCount,
                TotalPages = totalPages,
                CurrentPage = page,
                PageSize = pageSize,
                Data = playlists
            });
        }        // GET: api/Playlists/user
        [HttpGet("user")]
        [Authorize]
        public async Task<ActionResult<object>> GetUserPlaylists(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var query = _context.Playlists
                .Where(p => p.UserId == userId)
                .Include(p => p.User);

            // Apply pagination
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var playlists = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    p.UserId,
                    CreatorUsername = p.User.Username,
                    CoverImageUrl = !string.IsNullOrEmpty(p.CoverImageUrl) ? 
                        _fileStorage.GetFileUrl(p.CoverImageUrl) : null,
                    p.IsPublic,
                    p.CreatedAt,
                    p.UpdatedAt,
                    SongCount = p.PlaylistSongs.Count
                })
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return Ok(new
            {
                TotalCount = totalCount,
                TotalPages = totalPages,
                CurrentPage = page,
                PageSize = pageSize,
                Data = playlists
            });
        }

        // GET: api/Playlists/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetPlaylist(int id)
        {
            var playlist = await _context.Playlists
                .Include(p => p.User)
                .Include(p => p.PlaylistSongs)
                .ThenInclude(ps => ps.Song)
                .ThenInclude(s => s.Artist)
                .Include(p => p.PlaylistSongs)
                .ThenInclude(ps => ps.Song)
                .ThenInclude(s => s.Album)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (playlist == null)
            {
                return NotFound();
            }

            if (!playlist.IsPublic)
            {
                // Check if user is authorized to view this playlist
                if (!User.Identity!.IsAuthenticated)
                {
                    return Forbid();
                }

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (playlist.UserId != userId && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }
            }

            return new
            {
                playlist.Id,
                playlist.Name,
                playlist.Description,
                playlist.UserId,
                CreatorUsername = playlist.User.Username,
                CoverImageUrl = !string.IsNullOrEmpty(playlist.CoverImageUrl) ? 
                    _fileStorage.GetFileUrl(playlist.CoverImageUrl) : null,
                playlist.IsPublic,
                playlist.CreatedAt,
                playlist.UpdatedAt,
                Songs = playlist.PlaylistSongs
                    .OrderBy(ps => ps.Order)
                    .Select(ps => new
                    {
                        ps.SongId,
                        ps.Song.Title,
                        ArtistId = ps.Song.ArtistId,
                        ArtistName = ps.Song.Artist?.Name,
                        AlbumId = ps.Song.AlbumId,
                        AlbumTitle = ps.Song.Album?.Title,
                        ps.Song.Duration,
                        AudioUrl = !string.IsNullOrEmpty(ps.Song.AudioUrl) ? 
                            _fileStorage.GetFileUrl(ps.Song.AudioUrl) : null,
                        CoverImageUrl = !string.IsNullOrEmpty(ps.Song.CoverImageUrl) ? 
                            _fileStorage.GetFileUrl(ps.Song.CoverImageUrl) : 
                            (!string.IsNullOrEmpty(ps.Song.Album?.CoverImageUrl) ? 
                                _fileStorage.GetFileUrl(ps.Song.Album.CoverImageUrl) : null),
                        ps.Order,
                        ps.AddedAt
                    })
                    .ToList()
            };
        }

        // POST: api/Playlists
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Playlist>> CreatePlaylist([FromBody] PlaylistCreateDTO playlistDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var playlist = new Playlist
            {
                Name = playlistDto.Name,
                Description = playlistDto.Description,
                UserId = userId,
                IsPublic = playlistDto.IsPublic ?? true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Playlists.Add(playlist);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPlaylist), new { id = playlist.Id }, playlist);
        }        // PUT: api/Playlists/5
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdatePlaylist(int id, [FromBody] PlaylistUpdateDTO playlistDto)
        {
            _logger.LogInformation("UpdatePlaylist called with id: {Id}, playlistDto: {@PlaylistDto}", id, playlistDto);
            
            var playlist = await _context.Playlists
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id);
            
            if (playlist == null)
            {
                return NotFound();
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (playlist.UserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }            playlist.Name = playlistDto.Name ?? playlist.Name;
            playlist.Description = playlistDto.Description ?? playlist.Description;
            playlist.IsPublic = playlistDto.IsPublic ?? playlist.IsPublic;
            playlist.UpdatedAt = DateTime.UtcNow;try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Playlist {Id} updated successfully. New name: {Name}, Description: {Description}, IsPublic: {IsPublic}", 
                    id, playlist.Name, playlist.Description, playlist.IsPublic);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await PlaylistExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            // Return the updated playlist data
            var response = new
            {
                playlist.Id,
                playlist.Name,
                playlist.Description,
                playlist.UserId,
                CreatorUsername = playlist.User.Username,
                CoverImageUrl = !string.IsNullOrEmpty(playlist.CoverImageUrl) ? 
                    _fileStorage.GetFileUrl(playlist.CoverImageUrl) : null,
                playlist.IsPublic,
                playlist.CreatedAt,
                playlist.UpdatedAt
            };

            return Ok(response);
        }        // POST: api/Playlists/{playlistId}/add-song
        [HttpPost("{playlistId}/add-song")]
        [Authorize]
        public async Task<IActionResult> AddSongToPlaylist(int playlistId, [FromBody] AddSongToPlaylistDTO dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            // int cannot be null, so this check is unnecessary
            
            var playlist = await _context.Playlists
                .Include(p => p.PlaylistSongs)
                .FirstOrDefaultAsync(p => p.Id == playlistId);

            if (playlist == null)
            {
                return NotFound("Playlist not found.");
            }

            if (playlist.UserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid("You are not authorized to modify this playlist.");
            }

            var song = await _context.Songs.FindAsync(dto.SongId);
            if (song == null)
            {
                return NotFound("Song not found.");
            }

            var existingPlaylistSong = await _context.PlaylistSongs
                .FirstOrDefaultAsync(ps => ps.PlaylistId == playlistId && ps.SongId == dto.SongId);

            if (existingPlaylistSong != null)
            {
                return Ok("Song already in playlist.");
            }

            var newOrder = playlist.PlaylistSongs.Any() ? playlist.PlaylistSongs.Max(ps => ps.Order) + 1 : 1;

            var playlistSong = new PlaylistSong
            {
                PlaylistId = playlistId,
                SongId = dto.SongId,
                Order = newOrder,
                AddedAt = DateTime.UtcNow
            };

            _context.PlaylistSongs.Add(playlistSong);
            await _context.SaveChangesAsync();

            return Ok("Song added to playlist successfully.");
        }

        // DELETE: api/Playlists/5
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeletePlaylist(int id)
        {
            var playlist = await _context.Playlists.FindAsync(id);
            if (playlist == null)
            {
                return NotFound();
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (playlist.UserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            // Delete playlist cover if exists
            if (!string.IsNullOrEmpty(playlist.CoverImageUrl))
            {
                await _fileStorage.DeleteFileAsync(playlist.CoverImageUrl);
            }

            _context.Playlists.Remove(playlist);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/Playlists/5/songs
        [HttpPost("{id}/songs")]
        [Authorize]
        public async Task<IActionResult> AddSong(int id, [FromBody] PlaylistSongDTO songDto)
        {
            var playlist = await _context.Playlists
                .Include(p => p.PlaylistSongs)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (playlist == null)
            {
                return NotFound();
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (playlist.UserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var song = await _context.Songs.FindAsync(songDto.SongId);
            if (song == null)
            {
                return BadRequest("Song not found");
            }

            var existingSong = await _context.PlaylistSongs
                .FirstOrDefaultAsync(ps => ps.PlaylistId == id && ps.SongId == songDto.SongId);

            if (existingSong != null)
            {
                return BadRequest("Song already in playlist");
            }

            // Get the highest order number and add 1
            var order = playlist.PlaylistSongs.Any() ? 
                playlist.PlaylistSongs.Max(ps => ps.Order) + 1 : 0;

            _context.PlaylistSongs.Add(new PlaylistSong
            {
                PlaylistId = id,
                SongId = songDto.SongId,
                Order = order,
                AddedAt = DateTime.UtcNow
            });

            // Update playlist's modification time
            playlist.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok();
        }

        // DELETE: api/Playlists/5/songs/3
        [HttpDelete("{id}/songs/{songId}")]
        [Authorize]
        public async Task<IActionResult> RemoveSong(int id, int songId)
        {
            var playlist = await _context.Playlists.FindAsync(id);
            if (playlist == null)
            {
                return NotFound();
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (playlist.UserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var playlistSong = await _context.PlaylistSongs
                .FirstOrDefaultAsync(ps => ps.PlaylistId == id && ps.SongId == songId);

            if (playlistSong == null)
            {
                return NotFound("Song not in playlist");
            }

            _context.PlaylistSongs.Remove(playlistSong);
            
            // Reorder remaining songs
            var songsToReorder = await _context.PlaylistSongs
                .Where(ps => ps.PlaylistId == id && ps.Order > playlistSong.Order)
                .ToListAsync();

            foreach (var song in songsToReorder)
            {
                song.Order -= 1;
            }

            // Update playlist's modification time
            playlist.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok();
        }

        // PUT: api/Playlists/5/songs/reorder
        [HttpPut("{id}/songs/reorder")]
        [Authorize]
        public async Task<IActionResult> ReorderSongs(int id, [FromBody] ReorderSongsDTO reorderDto)
        {
            var playlist = await _context.Playlists.FindAsync(id);
            if (playlist == null)
            {
                return NotFound();
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (playlist.UserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            foreach (var item in reorderDto.SongOrders)
            {
                var playlistSong = await _context.PlaylistSongs
                    .FirstOrDefaultAsync(ps => ps.PlaylistId == id && ps.SongId == item.SongId);

                if (playlistSong != null)
                {
                    playlistSong.Order = item.Order;
                }
            }

            // Update playlist's modification time
            playlist.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok();
        }

        // POST: api/Playlists/5/cover
        [HttpPost("{id}/cover")]
        [Authorize]
        public async Task<ActionResult<string>> UploadCover(int id, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            var playlist = await _context.Playlists.FindAsync(id);
            if (playlist == null)
            {
                return NotFound();
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (playlist.UserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            // Delete old cover if exists
            if (!string.IsNullOrEmpty(playlist.CoverImageUrl))
            {
                await _fileStorage.DeleteFileAsync(playlist.CoverImageUrl);
            }

            // Save new cover
            var filePath = await _fileStorage.SaveFileAsync(file, "playlists");
            playlist.CoverImageUrl = filePath;
            playlist.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { coverImageUrl = _fileStorage.GetFileUrl(filePath) });
        }

        // POST: api/Playlists/5/copy
        [HttpPost("{id}/copy")]
        [Authorize]
        public async Task<ActionResult<object>> CopyPlaylist(int id)
        {
            var originalPlaylist = await _context.Playlists
                .Include(p => p.PlaylistSongs)
                .ThenInclude(ps => ps.Song)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (originalPlaylist == null)
            {
                return NotFound("Playlist not found");
            }

            // Check if the playlist is public or user is admin/owner
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (!originalPlaylist.IsPublic && originalPlaylist.UserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid("This playlist is not public and you don't have permission to copy it");
            }

            // Create a copy of the playlist
            var copiedPlaylist = new Playlist
            {
                Name = $"{originalPlaylist.Name} (Copy)",
                Description = originalPlaylist.Description,
                UserId = userId,
                IsPublic = false, // Copied playlists default to private
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CoverImageUrl = originalPlaylist.CoverImageUrl // Keep the same cover image
            };

            _context.Playlists.Add(copiedPlaylist);
            await _context.SaveChangesAsync();

            // Copy all songs from the original playlist
            foreach (var originalSong in originalPlaylist.PlaylistSongs.OrderBy(ps => ps.Order))
            {
                _context.PlaylistSongs.Add(new PlaylistSong
                {
                    PlaylistId = copiedPlaylist.Id,
                    SongId = originalSong.SongId,
                    Order = originalSong.Order,
                    AddedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = copiedPlaylist.Id,
                name = copiedPlaylist.Name,
                message = "Playlist copied successfully"
            });
        }

        private async Task<bool> PlaylistExists(int id)
        {
            return await _context.Playlists.AnyAsync(p => p.Id == id);
        }
    }

    public class PlaylistCreateDTO
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool? IsPublic { get; set; }
    }    public class PlaylistUpdateDTO
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool? IsPublic { get; set; }
    }

    public class PlaylistSongDTO
    {
        public int SongId { get; set; }
    }

    public class ReorderSongsDTO
    {
        public List<SongOrderItem> SongOrders { get; set; } = new List<SongOrderItem>();
    }

    public class SongOrderItem
    {
        public int SongId { get; set; }
        public int Order { get; set; }
    }

    public class AddSongToPlaylistDTO
    {
        public int SongId { get; set; }
    }
}
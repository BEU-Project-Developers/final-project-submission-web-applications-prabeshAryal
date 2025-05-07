using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MusicAppBackend.Data;
using MusicAppBackend.Services;
using System.Linq;
using System.Threading.Tasks;

namespace MusicAppBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly MusicDbContext _context;
        private readonly IFileStorageService _fileStorage;

        public SearchController(MusicDbContext context, IFileStorageService fileStorage)
        {
            _context = context;
            _fileStorage = fileStorage;
        }

        // GET: api/Search?query=something
        [HttpGet]
        public async Task<ActionResult> Search([FromQuery] string query, [FromQuery] int limit = 10)
        {
            if (string.IsNullOrEmpty(query) || query.Length < 2)
            {
                return BadRequest("Search query must be at least 2 characters");
            }

            var loweredQuery = query.ToLower();

            // Search songs
            var songs = await _context.Songs
                .Include(s => s.Artist)
                .Include(s => s.Album)
                .Where(s => s.Title.ToLower().Contains(loweredQuery))
                .Take(limit)
                .Select(s => new
                {
                    Id = s.Id,
                    Title = s.Title,
                    Type = "song",
                    ArtistName = s.Artist != null ? s.Artist.Name : null,
                    AlbumTitle = s.Album != null ? s.Album.Title : null,
                    Duration = s.Duration,
                    CoverImageUrl = !string.IsNullOrEmpty(s.CoverImageUrl) ?
                        _fileStorage.GetFileUrl(s.CoverImageUrl) :
                        (!string.IsNullOrEmpty(s.Album!.CoverImageUrl) ?
                            _fileStorage.GetFileUrl(s.Album.CoverImageUrl) : null),
                })
                .ToListAsync();

            // Search artists
            var artists = await _context.Artists
                .Where(a => a.Name.ToLower().Contains(loweredQuery))
                .Take(limit)
                .Select(a => new
                {
                    Id = a.Id,
                    Name = a.Name,
                    Type = "artist",
                    ImageUrl = !string.IsNullOrEmpty(a.ImageUrl) ?
                        _fileStorage.GetFileUrl(a.ImageUrl) : null,
                    Genre = a.Genre,
                })
                .ToListAsync();

            // Search albums
            var albums = await _context.Albums
                .Include(a => a.Artist)
                .Where(a => a.Title.ToLower().Contains(loweredQuery))
                .Take(limit)
                .Select(a => new
                {
                    Id = a.Id,
                    Title = a.Title,
                    Type = "album",
                    ArtistName = a.Artist.Name,
                    Year = a.Year,
                    CoverImageUrl = !string.IsNullOrEmpty(a.CoverImageUrl) ?
                        _fileStorage.GetFileUrl(a.CoverImageUrl) : null,
                })
                .ToListAsync();

            // Search playlists (only public ones)
            var playlists = await _context.Playlists
                .Include(p => p.User)
                .Where(p => p.IsPublic && p.Name.ToLower().Contains(loweredQuery))
                .Take(limit)
                .Select(p => new
                {
                    Id = p.Id,
                    Name = p.Name,
                    Type = "playlist",
                    CreatorUsername = p.User.Username,
                    SongCount = p.PlaylistSongs.Count,
                    CoverImageUrl = !string.IsNullOrEmpty(p.CoverImageUrl) ?
                        _fileStorage.GetFileUrl(p.CoverImageUrl) : null,
                })
                .ToListAsync();

            return Ok(new
            {
                Query = query,
                Results = new
                {
                    Songs = songs,
                    Artists = artists,
                    Albums = albums,
                    Playlists = playlists
                }
            });
        }

        // GET: api/Search/songs?query=something
        [HttpGet("songs")]
        public async Task<ActionResult> SearchSongs([FromQuery] string query, [FromQuery] int limit = 20)
        {
            if (string.IsNullOrEmpty(query) || query.Length < 2)
            {
                return BadRequest("Search query must be at least 2 characters");
            }

            var loweredQuery = query.ToLower();

            var songs = await _context.Songs
                .Include(s => s.Artist)
                .Include(s => s.Album)
                .Where(s => s.Title.ToLower().Contains(loweredQuery) ||
                           (s.Artist != null && s.Artist.Name.ToLower().Contains(loweredQuery)) ||
                           (s.Album != null && s.Album.Title.ToLower().Contains(loweredQuery)))
                .Take(limit)
                .Select(s => new
                {
                    Id = s.Id,
                    Title = s.Title,
                    ArtistId = s.ArtistId,
                    ArtistName = s.Artist != null ? s.Artist.Name : null,
                    AlbumId = s.AlbumId,
                    AlbumTitle = s.Album != null ? s.Album.Title : null,
                    Duration = s.Duration,
                    AudioUrl = !string.IsNullOrEmpty(s.AudioUrl) ?
                        _fileStorage.GetFileUrl(s.AudioUrl) : null,
                    CoverImageUrl = !string.IsNullOrEmpty(s.CoverImageUrl) ?
                        _fileStorage.GetFileUrl(s.CoverImageUrl) :
                        (!string.IsNullOrEmpty(s.Album!.CoverImageUrl) ?
                            _fileStorage.GetFileUrl(s.Album.CoverImageUrl) : null),
                    TrackNumber = s.TrackNumber,
                    Genre = s.Genre,
                    PlayCount = s.PlayCount
                })
                .ToListAsync();

            return Ok(songs);
        }

        // GET: api/Search/artists?query=something
        [HttpGet("artists")]
        public async Task<ActionResult> SearchArtists([FromQuery] string query, [FromQuery] int limit = 20)
        {
            if (string.IsNullOrEmpty(query) || query.Length < 2)
            {
                return BadRequest("Search query must be at least 2 characters");
            }

            var loweredQuery = query.ToLower();

            var artists = await _context.Artists
                .Where(a => a.Name.ToLower().Contains(loweredQuery) ||
                          (a.Bio != null && a.Bio.ToLower().Contains(loweredQuery)) ||
                          (a.Genre != null && a.Genre.ToLower().Contains(loweredQuery)))
                .Take(limit)
                .Select(a => new
                {
                    Id = a.Id,
                    Name = a.Name,
                    Bio = a.Bio,
                    ImageUrl = !string.IsNullOrEmpty(a.ImageUrl) ?
                        _fileStorage.GetFileUrl(a.ImageUrl) : null,
                    Country = a.Country,
                    Genre = a.Genre,
                    MonthlyListeners = a.MonthlyListeners,
                    IsActive = a.IsActive
                })
                .ToListAsync();

            return Ok(artists);
        }

        // GET: api/Search/albums?query=something
        [HttpGet("albums")]
        public async Task<ActionResult> SearchAlbums([FromQuery] string query, [FromQuery] int limit = 20)
        {
            if (string.IsNullOrEmpty(query) || query.Length < 2)
            {
                return BadRequest("Search query must be at least 2 characters");
            }

            var loweredQuery = query.ToLower();

            var albums = await _context.Albums
                .Include(a => a.Artist)
                .Where(a => a.Title.ToLower().Contains(loweredQuery) ||
                           (a.Description != null && a.Description.ToLower().Contains(loweredQuery)) ||
                           (a.Genre != null && a.Genre.ToLower().Contains(loweredQuery)) ||
                           a.Artist.Name.ToLower().Contains(loweredQuery))
                .Take(limit)
                .Select(a => new
                {
                    Id = a.Id,
                    Title = a.Title,
                    ArtistId = a.ArtistId,
                    ArtistName = a.Artist.Name,
                    CoverImageUrl = !string.IsNullOrEmpty(a.CoverImageUrl) ?
                        _fileStorage.GetFileUrl(a.CoverImageUrl) : null,
                    Year = a.Year,
                    Description = a.Description,
                    Genre = a.Genre,
                    ReleaseDate = a.ReleaseDate,
                    TotalTracks = a.TotalTracks
                })
                .ToListAsync();

            return Ok(albums);
        }

        // GET: api/Search/playlists?query=something
        [HttpGet("playlists")]
        public async Task<ActionResult> SearchPlaylists([FromQuery] string query, [FromQuery] int limit = 20)
        {
            if (string.IsNullOrEmpty(query) || query.Length < 2)
            {
                return BadRequest("Search query must be at least 2 characters");
            }

            var loweredQuery = query.ToLower();

            var playlists = await _context.Playlists
                .Include(p => p.User)
                .Where(p => p.IsPublic && 
                          (p.Name.ToLower().Contains(loweredQuery) ||
                          (p.Description != null && p.Description.ToLower().Contains(loweredQuery)) ||
                          p.User.Username.ToLower().Contains(loweredQuery)))
                .Take(limit)
                .Select(p => new
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    UserId = p.UserId,
                    CreatorUsername = p.User.Username,
                    CoverImageUrl = !string.IsNullOrEmpty(p.CoverImageUrl) ?
                        _fileStorage.GetFileUrl(p.CoverImageUrl) : null,
                    IsPublic = p.IsPublic,
                    CreatedAt = p.CreatedAt,
                    SongCount = p.PlaylistSongs.Count
                })
                .ToListAsync();

            return Ok(playlists);
        }
    }
} 
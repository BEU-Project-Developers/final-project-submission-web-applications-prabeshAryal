// Controllers/ArtistsController.cs
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
    public class ArtistsController : ControllerBase
    {
        private readonly MusicDbContext _context;
        private readonly IFileStorageService _fileStorage;

        public ArtistsController(MusicDbContext context, IFileStorageService fileStorage)
        {
            _context = context;
            _fileStorage = fileStorage;
        }

        // GET: api/Artists
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetArtists(
            [FromQuery] string? genre,
            [FromQuery] string? country,
            [FromQuery] string? search,
            [FromQuery] string? sortBy,
            [FromQuery] bool desc = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            IQueryable<Artist> query = _context.Artists;

            // Apply filters
            if (!string.IsNullOrEmpty(genre))
            {
                query = query.Where(a => a.Genre == genre);
            }

            if (!string.IsNullOrEmpty(country))
            {
                query = query.Where(a => a.Country == country);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(a => a.Name.Contains(search) || a.Bio!.Contains(search));
            }

            // Apply sorting
            if (!string.IsNullOrEmpty(sortBy))
            {
                switch (sortBy.ToLower())
                {
                    case "name":
                        query = desc ? query.OrderByDescending(a => a.Name) : query.OrderBy(a => a.Name);
                        break;
                    case "country":
                        query = desc ? query.OrderByDescending(a => a.Country) : query.OrderBy(a => a.Country);
                        break;
                    case "formeddate":
                        query = desc ? query.OrderByDescending(a => a.FormedDate) : query.OrderBy(a => a.FormedDate);
                        break;
                    case "monthlylisteners":
                        query = desc ? query.OrderByDescending(a => a.MonthlyListeners) : query.OrderBy(a => a.MonthlyListeners);
                        break;
                    default:
                        query = desc ? query.OrderByDescending(a => a.Id) : query.OrderBy(a => a.Id);
                        break;
                }
            }
            else
            {
                // Default sort
                query = query.OrderBy(a => a.Name);
            }

            // Apply pagination
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var artists = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new
                {
                    a.Id,
                    a.Name,
                    a.Country,
                    a.Genre,
                    a.FormedDate,
                    a.MonthlyListeners,
                    a.IsActive,
                    ImageUrl = !string.IsNullOrEmpty(a.ImageUrl) ? 
                        _fileStorage.GetFileUrl(a.ImageUrl) : null
                })
                .ToListAsync();

            return Ok(new
            {
                TotalCount = totalCount,
                TotalPages = totalPages,
                CurrentPage = page,
                PageSize = pageSize,
                Data = artists
            });
        }

        // GET: api/Artists/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetArtist(int id)
        {
            var artist = await _context.Artists
                .Include(a => a.Albums)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (artist == null)
            {
                return NotFound();
            }

            return new
            {
                artist.Id,
                artist.Name,
                artist.Bio,
                ImageUrl = !string.IsNullOrEmpty(artist.ImageUrl) ? 
                    _fileStorage.GetFileUrl(artist.ImageUrl) : null,
                artist.Country,
                artist.Genre,
                artist.FormedDate,
                artist.MonthlyListeners,
                artist.IsActive,
                Albums = artist.Albums.Select(a => new
                {
                    a.Id,
                    a.Title,
                    CoverImageUrl = !string.IsNullOrEmpty(a.CoverImageUrl) ? 
                        _fileStorage.GetFileUrl(a.CoverImageUrl) : null,
                    a.Year,
                    a.ReleaseDate
                }).OrderByDescending(a => a.ReleaseDate).ToList()
            };
        }

        // POST: api/Artists
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Artist>> CreateArtist([FromBody] ArtistCreateDTO artistDto)
        {
            var artist = new Artist
            {
                Name = artistDto.Name,
                Bio = artistDto.Bio,
                Country = artistDto.Country,
                Genre = artistDto.Genre,
                FormedDate = artistDto.FormedDate,
                MonthlyListeners = artistDto.MonthlyListeners,
                IsActive = artistDto.IsActive ?? true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };            _context.Artists.Add(artist);
            await _context.SaveChangesAsync();

            // Return a simple DTO to avoid circular references
            return CreatedAtAction(nameof(GetArtist), new { id = artist.Id }, new
            {
                artist.Id,
                artist.Name,
                artist.Bio,
                artist.Country,
                artist.Genre,
                artist.FormedDate,
                artist.MonthlyListeners,
                artist.IsActive,
                artist.CreatedAt,
                artist.UpdatedAt
            });
        }

        // PUT: api/Artists/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateArtist(int id, [FromBody] ArtistUpdateDTO artistDto)
        {
            var artist = await _context.Artists.FindAsync(id);
            if (artist == null)
            {
                return NotFound();
            }

            artist.Name = artistDto.Name ?? artist.Name;
            artist.Bio = artistDto.Bio ?? artist.Bio;
            artist.Country = artistDto.Country ?? artist.Country;
            artist.Genre = artistDto.Genre ?? artist.Genre;
            artist.FormedDate = artistDto.FormedDate ?? artist.FormedDate;
            artist.MonthlyListeners = artistDto.MonthlyListeners ?? artist.MonthlyListeners;
            artist.IsActive = artistDto.IsActive ?? artist.IsActive;
            artist.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ArtistExists(id))
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

        // DELETE: api/Artists/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteArtist(int id)
        {
            var artist = await _context.Artists
                .Include(a => a.Albums)
                .FirstOrDefaultAsync(a => a.Id == id);
                
            if (artist == null)
            {
                return NotFound();
            }

            // Delete artist image if exists
            if (!string.IsNullOrEmpty(artist.ImageUrl))
            {
                await _fileStorage.DeleteFileAsync(artist.ImageUrl);
            }

            // Remove the artist
            _context.Artists.Remove(artist);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/Artists/5/image
        [HttpPost("{id}/image")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<string>> UploadImage(int id, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            var artist = await _context.Artists.FindAsync(id);
            if (artist == null)
            {
                return NotFound();
            }

            // Delete old image if exists
            if (!string.IsNullOrEmpty(artist.ImageUrl))
            {
                await _fileStorage.DeleteFileAsync(artist.ImageUrl);
            }

            // Save new image
            var filePath = await _fileStorage.SaveFileAsync(file, "artists");
            artist.ImageUrl = filePath;
            artist.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { imageUrl = _fileStorage.GetFileUrl(filePath) });
        }

        // GET: api/Artists/genres
        [HttpGet("genres")]
        public async Task<ActionResult<IEnumerable<string>>> GetGenres()
        {
            var genres = await _context.Artists
                .Where(a => a.Genre != null)
                .Select(a => a.Genre)
                .Distinct()
                .OrderBy(g => g)
                .ToListAsync();

            return genres!;
        }

        // GET: api/Artists/countries
        [HttpGet("countries")]
        public async Task<ActionResult<IEnumerable<string>>> GetCountries()
        {
            var countries = await _context.Artists
                .Where(a => a.Country != null)
                .Select(a => a.Country)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            return countries!;
        }

        private async Task<bool> ArtistExists(int id)
        {
            return await _context.Artists.AnyAsync(a => a.Id == id);
        }
    }

    public class ArtistCreateDTO
    {
        public string Name { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? Country { get; set; }
        public string? Genre { get; set; }
        public DateTime? FormedDate { get; set; }
        public int? MonthlyListeners { get; set; }
        public bool? IsActive { get; set; }
    }

    public class ArtistUpdateDTO
    {
        public string? Name { get; set; }
        public string? Bio { get; set; }
        public string? Country { get; set; }
        public string? Genre { get; set; }
        public DateTime? FormedDate { get; set; }
        public int? MonthlyListeners { get; set; }
        public bool? IsActive { get; set; }
    }
}
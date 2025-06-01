using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MusicAppBackend.Data;
using MusicAppBackend.DTOs;
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
    [Authorize]
    public class UsersController : BaseController
    {
        private readonly IFileStorageService _fileStorage;

        public UsersController(MusicDbContext context, IFileStorageService fileStorage, ILogger<UsersController> logger)
            : base(context, logger)
        {
            _fileStorage = fileStorage;
        }

        // GET: api/Users
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<UserDTO>>> GetUsers()
        {
            var users = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)                .Select(u => new UserDTO
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    ProfileImageUrl = u.ProfileImageUrl,
                    Bio = u.Bio,
                    Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
                })
                .ToListAsync();

            return users;
        }        // GET: api/Users/top-artists
        [HttpGet("top-artists")]
        public async Task<ActionResult<IEnumerable<object>>> GetCurrentUserTopArtists()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Get user's top artists based on actual play data
            var topArtists = await _context.UserSongPlays
                .Where(usp => usp.UserId == userId)
                .Include(usp => usp.Song)
                .ThenInclude(s => s.Artist)
                .Where(usp => usp.Song.Artist != null)
                .GroupBy(usp => usp.Song.Artist!)
                .Select(g => new
                {
                    Id = g.Key.Id,
                    Name = g.Key.Name,
                    Bio = g.Key.Bio,
                    ImageUrl = g.Key.ImageUrl,
                    Country = g.Key.Country,
                    Genre = g.Key.Genre,
                    FormedDate = g.Key.FormedDate,
                    MonthlyListeners = g.Key.MonthlyListeners ?? 0,
                    IsActive = g.Key.IsActive,
                    PlayCount = g.Count(), // Number of plays by this artist
                    FollowersCount = 0 // Default value for compatibility
                })
                .OrderByDescending(a => a.PlayCount)
                .Take(10) // Return top 10 artists
                .ToListAsync();

            return Ok(topArtists);
        }        // GET: api/Users/statistics
        [HttpGet("statistics")]
        public async Task<ActionResult<object>> GetCurrentUserStatistics()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Total listening time - get all durations and calculate minutes client-side
            var userPlays = await _context.UserSongPlays
                .Where(usp => usp.UserId == userId && usp.Duration.HasValue)
                .Select(usp => usp.Duration!.Value)
                .ToListAsync();
            
            var totalListeningTime = userPlays.Sum(duration => duration.TotalMinutes);

            // Top genre
            var topGenre = await _context.UserSongPlays
                .Where(usp => usp.UserId == userId)
                .Include(usp => usp.Song)
                .Where(usp => !string.IsNullOrEmpty(usp.Song.Genre))
                .GroupBy(usp => usp.Song.Genre)
                .Select(g => new { Genre = g.Key, PlayCount = g.Count() })
                .OrderByDescending(g => g.PlayCount)
                .FirstOrDefaultAsync();

            // Favorite artist (most played)
            var favoriteArtist = await _context.UserSongPlays
                .Where(usp => usp.UserId == userId)
                .Include(usp => usp.Song)
                .ThenInclude(s => s.Artist)
                .Where(usp => usp.Song.Artist != null)
                .GroupBy(usp => usp.Song.Artist!)
                .Select(g => new { Artist = g.Key.Name, PlayCount = g.Count() })
                .OrderByDescending(g => g.PlayCount)
                .FirstOrDefaultAsync();

            // Recently played songs
            var recentlyPlayed = await _context.UserSongPlays
                .Where(usp => usp.UserId == userId)
                .Include(usp => usp.Song)
                .ThenInclude(s => s.Artist)
                .OrderByDescending(usp => usp.PlayedAt)
                .Take(5)
                .Select(usp => new
                {
                    Id = usp.Song.Id,
                    Title = usp.Song.Title,
                    Artist = usp.Song.Artist != null ? usp.Song.Artist.Name : "Unknown Artist",
                    PlayedAt = usp.PlayedAt,
                    Duration = usp.Duration
                })
                .ToListAsync();

            // Recent activity (play history)
            var recentActivity = await _context.UserSongPlays
                .Where(usp => usp.UserId == userId)
                .Include(usp => usp.Song)
                .ThenInclude(s => s.Artist)
                .OrderByDescending(usp => usp.PlayedAt)
                .Take(10)
                .Select(usp => new
                {
                    Id = usp.Id,
                    Action = "Played",
                    SongTitle = usp.Song.Title,
                    ArtistName = usp.Song.Artist != null ? usp.Song.Artist.Name : "Unknown Artist",
                    Timestamp = usp.PlayedAt,
                    Duration = usp.Duration
                })
                .ToListAsync();

            return Ok(new
            {
                TotalListeningTimeMinutes = totalListeningTime,
                TopGenre = topGenre?.Genre ?? "No data",
                FavoriteArtist = favoriteArtist?.Artist ?? "No data",
                RecentlyPlayed = recentlyPlayed,
                RecentActivity = recentActivity,
                TotalPlays = await _context.UserSongPlays.CountAsync(usp => usp.UserId == userId)
            });
        }

        // GET: api/Users/listening-history
        [HttpGet("listening-history")]
        public async Task<ActionResult<object>> GetListeningHistory(int page = 1, int pageSize = 20)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var totalCount = await _context.UserSongPlays
                .CountAsync(usp => usp.UserId == userId);

            var history = await _context.UserSongPlays
                .Where(usp => usp.UserId == userId)
                .Include(usp => usp.Song)
                .ThenInclude(s => s.Artist)
                .Include(usp => usp.Song)
                .ThenInclude(s => s.Album)
                .OrderByDescending(usp => usp.PlayedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(usp => new
                {
                    Id = usp.Id,
                    Song = new
                    {
                        Id = usp.Song.Id,
                        Title = usp.Song.Title,
                        Artist = usp.Song.Artist != null ? usp.Song.Artist.Name : "Unknown Artist",
                        Album = usp.Song.Album != null ? usp.Song.Album.Title : null,
                        Duration = usp.Song.Duration,
                        Genre = usp.Song.Genre,
                        CoverImageUrl = usp.Song.CoverImageUrl
                    },
                    PlayedAt = usp.PlayedAt,
                    ListenDuration = usp.Duration
                })
                .ToListAsync();

            return Ok(new
            {
                Data = history,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                TotalCount = totalCount
            });
        }        // GET: api/Users/profile
        [HttpGet("profile")]
        public async Task<ActionResult<UserDTO>> GetCurrentUser()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound();
            }
            return new UserDTO
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ProfileImageUrl = user.ProfileImageUrl,
                Bio = user.Bio,
                Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
            };
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDTO>> GetUser(int id)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (user.Id != currentUserId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }            return new UserDTO
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ProfileImageUrl = user.ProfileImageUrl,
                Bio = user.Bio,
                Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
            };
        }

        // PUT: api/Users/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDTO updateUserDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (id != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }            user.FirstName = updateUserDto.FirstName ?? user.FirstName;
            user.LastName = updateUserDto.LastName ?? user.LastName;
            user.Username = updateUserDto.Username ?? user.Username;
            user.Bio = updateUserDto.Bio ?? user.Bio;
            user.ProfileImageUrl = updateUserDto.ProfileImageUrl ?? user.ProfileImageUrl;
            user.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await UserExists(id))
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

        // POST: api/Users/profile-image
        [HttpPost("profile-image")]
        public async Task<ActionResult<string>> UploadProfileImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            // Delete old image if exists
            if (!string.IsNullOrEmpty(user.ProfileImageUrl))
            {
                await _fileStorage.DeleteFileAsync(user.ProfileImageUrl);
            }            // Save new image
            var filePath = await _fileStorage.SaveFileAsync(file, "profiles");
            user.ProfileImageUrl = filePath;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { filePath = _fileStorage.GetFileUrl(filePath) });
        }

        // GET: api/Users/{id}/followers
        [HttpGet("{id}/followers")]
        public async Task<ActionResult<IEnumerable<UserDTO>>> GetFollowers(int id)
        {
            var followers = await _context.UserFollowers
                .Where(uf => uf.UserId == id)
                .Include(uf => uf.Follower)
                .ThenInclude(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)                .Select(uf => new UserDTO
                {
                    Id = uf.Follower.Id,
                    Username = uf.Follower.Username,
                    Email = uf.Follower.Email,
                    FirstName = uf.Follower.FirstName,
                    LastName = uf.Follower.LastName,
                    ProfileImageUrl = uf.Follower.ProfileImageUrl,
                    Bio = uf.Follower.Bio,
                    Roles = uf.Follower.UserRoles.Select(ur => ur.Role.Name).ToList()
                })
                .ToListAsync();

            return followers;
        }

        // GET: api/Users/{id}/following
        [HttpGet("{id}/following")]
        public async Task<ActionResult<IEnumerable<UserDTO>>> GetFollowing(int id)
        {
            var following = await _context.UserFollowers
                .Where(uf => uf.FollowerId == id)
                .Include(uf => uf.User)
                .ThenInclude(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)                .Select(uf => new UserDTO
                {
                    Id = uf.User.Id,
                    Username = uf.User.Username,
                    Email = uf.User.Email,
                    FirstName = uf.User.FirstName,
                    LastName = uf.User.LastName,
                    ProfileImageUrl = uf.User.ProfileImageUrl,
                    Bio = uf.User.Bio,
                    Roles = uf.User.UserRoles.Select(ur => ur.Role.Name).ToList()
                })
                .ToListAsync();

            return following;
        }

        // POST: api/Users/{id}/follow
        [HttpPost("{id}/follow")]
        public async Task<IActionResult> FollowUser(int id)
        {
            var followerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (followerId == id)
            {
                return BadRequest("Cannot follow yourself");
            }

            var userToFollow = await _context.Users.FindAsync(id);
            if (userToFollow == null)
            {
                return NotFound();
            }

            var existingFollow = await _context.UserFollowers
                .FirstOrDefaultAsync(uf => uf.UserId == id && uf.FollowerId == followerId);

            if (existingFollow != null)
            {
                return BadRequest("Already following this user");
            }

            _context.UserFollowers.Add(new UserFollower
            {
                UserId = id,
                FollowerId = followerId,
                FollowedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok();
        }

        // DELETE: api/Users/{id}/unfollow
        [HttpDelete("{id}/unfollow")]
        public async Task<IActionResult> UnfollowUser(int id)
        {
            var followerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var follow = await _context.UserFollowers
                .FirstOrDefaultAsync(uf => uf.UserId == id && uf.FollowerId == followerId);

            if (follow == null)
            {
                return NotFound("Not following this user");
            }

            _context.UserFollowers.Remove(follow);
            await _context.SaveChangesAsync();
            return Ok();
        }        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .Include(u => u.Playlists)
                .Include(u => u.Favorites)
                .Include(u => u.Followers)
                .Include(u => u.Following)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            // Don't allow deletion of the current admin user
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (user.Id == currentUserId)
            {
                return BadRequest("Cannot delete your own account");
            }

            // Delete profile image if exists
            if (!string.IsNullOrEmpty(user.ProfileImageUrl))
            {
                await _fileStorage.DeleteFileAsync(user.ProfileImageUrl);
            }

            // Remove user (cascade delete will handle related records)
            _context.Users.Remove(user);
            await _context.SaveChangesAsync(); return NoContent();
        }

        private async Task<bool> UserExists(int id)
        {
            return await _context.Users.AnyAsync(u => u.Id == id);
        }
    }    public class UpdateUserDTO
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Username { get; set; }
        public string? Bio { get; set; }
        public string? ProfileImageUrl { get; set; }
    }
}
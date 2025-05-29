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
                .ThenInclude(ur => ur.Role)
                .Select(u => new UserDTO
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    ProfileImageUrl = u.ProfileImageUrl,
                    Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
                })
                .ToListAsync();

            return users;
        }        // GET: api/Users/top-artists
        [HttpGet("top-artists")]
        public async Task<ActionResult<IEnumerable<object>>> GetCurrentUserTopArtists()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Get user's favorite songs and group by artist to find top artists
            var topArtists = await _context.UserFavorites
                .Where(uf => uf.UserId == userId)
                .Include(uf => uf.Song)
                .ThenInclude(s => s.Artist)
                .Where(uf => uf.Song.Artist != null)
                .GroupBy(uf => uf.Song.Artist!)
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
                    PlayCount = g.Count(), // Number of favorite songs by this artist
                    FollowersCount = 0 // Default value for compatibility
                })
                .OrderByDescending(a => a.PlayCount)
                .Take(10) // Return top 10 artists
                .ToListAsync();

            return Ok(topArtists);
        }

        // GET: api/Users/profile
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
            }            return new UserDTO
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ProfileImageUrl = user.ProfileImageUrl,
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
            }

            return new UserDTO
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ProfileImageUrl = user.ProfileImageUrl,
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
            }

            user.FirstName = updateUserDto.FirstName ?? user.FirstName;
            user.LastName = updateUserDto.LastName ?? user.LastName;
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
            }

            // Save new image
            var filePath = await _fileStorage.SaveFileAsync(file, "profiles");
            user.ProfileImageUrl = filePath;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { profileImageUrl = _fileStorage.GetFileUrl(filePath) });
        }

        // GET: api/Users/{id}/followers
        [HttpGet("{id}/followers")]
        public async Task<ActionResult<IEnumerable<UserDTO>>> GetFollowers(int id)
        {
            var followers = await _context.UserFollowers
                .Where(uf => uf.UserId == id)
                .Include(uf => uf.Follower)
                .ThenInclude(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Select(uf => new UserDTO
                {
                    Id = uf.Follower.Id,
                    Username = uf.Follower.Username,
                    Email = uf.Follower.Email,
                    FirstName = uf.Follower.FirstName,
                    LastName = uf.Follower.LastName,
                    ProfileImageUrl = uf.Follower.ProfileImageUrl,
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
                .ThenInclude(ur => ur.Role)
                .Select(uf => new UserDTO
                {
                    Id = uf.User.Id,
                    Username = uf.User.Username,
                    Email = uf.User.Email,
                    FirstName = uf.User.FirstName,
                    LastName = uf.User.LastName,
                    ProfileImageUrl = uf.User.ProfileImageUrl,
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
            await _context.SaveChangesAsync();            return NoContent();
        }

        private async Task<bool> UserExists(int id)
        {
            return await _context.Users.AnyAsync(u => u.Id == id);
        }
    }

    public class UpdateUserDTO
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }
}
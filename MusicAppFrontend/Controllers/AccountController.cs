using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MusicApp.Models;
using MusicApp.Models.DTOs;
using MusicApp.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MusicApp.Data;
using Microsoft.JSInterop;
using System.Text.Json;
using MusicApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace MusicApp.Controllers
{
    public class AccountController : BaseAppController
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthService _authService;

        public AccountController(ApplicationDbContext context, AuthService authService, ApiService apiService, ILogger<AccountController> logger)
            : base(apiService, logger)
        {
            _context = context;
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Dashboard");
            }
            
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Please check your input and try again." });
                }
                return View(model);
            }

            return await SafeApiAction(async () =>
            {
                var success = await _authService.LoginAsync(model.UsernameOrEmail, model.Password, model.RememberMe);

                if (success)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = true, redirectUrl = Url.Action("Index", "Home") });
                    }
                    return RedirectToAction("Index", "Home");
                }

                // Login failed
                var errorMessage = "Invalid username/email or password. Please try again.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = errorMessage });
                }

                ModelState.AddModelError(string.Empty, errorMessage);
                return View(model);
            },
            () => {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "An error occurred during login. Please try again." });
                }
                ModelState.AddModelError(string.Empty, "An error occurred during login");
                return View(model);
            },
            "An error occurred during login. Please try again.",
            "AccountController.Login POST");
        }

        [HttpGet]
        public IActionResult Register(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Dashboard");
            }
            
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Please check your input and try again." });
                }
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            return await SafeApiAction(async () =>
            {
                var success = await _authService.RegisterAsync(model);
                
                if (success)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = true });
                    }

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);
                    else
                        return RedirectToAction("Dashboard");
                }

                // Registration failed
                var errorMessage = "Registration failed. Email or username may already be in use.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = errorMessage });
                }
                
                ModelState.AddModelError(string.Empty, errorMessage);
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            },
            () => {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "An error occurred during registration. Please try again." });
                }
                ModelState.AddModelError(string.Empty, "An error occurred during registration");
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            },
            "An error occurred during registration. Please try again.",
            "AccountController.Register POST");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _authService.LogoutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", new { returnUrl = Url.Action("Dashboard") });
            }

            return await SafeApiAction(async () =>
            {
                // Try to get user profile from the API
                var userProfile = await SafeApiCall(
                    async () => await _apiService.GetAsync<UserDto>("api/Users/profile"),
                    (UserDto)null,
                    "Unable to load user profile",
                    "AccountController.Dashboard - Loading user profile"
                );
                
                // Create and populate ProfileViewModel
                var profile = new ProfileViewModel();
                
                if (userProfile != null)
                {
                    // Create a User object from UserDto
                    profile.User = new User
                    {
                        Id = userProfile.Id,
                        Username = userProfile.Username,
                        Email = userProfile.Email,
                        FirstName = userProfile.FirstName, 
                        LastName = userProfile.LastName,
                        ProfileImageUrl = userProfile.ProfileImageUrl,
                        CreatedAt = userProfile.CreatedAt
                    };
                    
                    // Get user statistics (now dynamic!)
                    var statisticsResponse = await SafeApiCall(
                        async () => await _apiService.GetAsync<object>("api/Users/statistics"),
                        (object)null,
                        "Unable to load user statistics",
                        "AccountController.Dashboard - Loading user statistics"
                    );

                    if (statisticsResponse != null)
                    {
                        // Convert to JsonElement for safe property access
                        var statsJson = JsonSerializer.Serialize(statisticsResponse);
                        var statsObj = JsonSerializer.Deserialize<JsonElement>(statsJson);
                        
                        // Set listening time
                        if (statsObj.TryGetProperty("totalListeningTimeMinutes", out JsonElement listeningTime))
                        {
                            var minutes = listeningTime.GetInt64();
                            var hours = minutes / 60;
                            var remainingMinutes = minutes % 60;
                            profile.TotalListeningTime = hours > 0 ? $"{hours}h {remainingMinutes}m" : $"{minutes}m";
                        }
                        
                        // Set top genre
                        if (statsObj.TryGetProperty("topGenre", out JsonElement topGenre))
                        {
                            profile.TopGenre = topGenre.GetString() ?? "No data";
                        }
                        
                        // Set favorite artist
                        if (statsObj.TryGetProperty("favoriteArtist", out JsonElement favoriteArtist))
                        {
                            profile.FavoriteArtist = favoriteArtist.GetString() ?? "No data";
                        }
                        
                        // Populate recently played tracks
                        if (statsObj.TryGetProperty("recentlyPlayed", out JsonElement recentlyPlayedElement) && 
                            recentlyPlayedElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var track in recentlyPlayedElement.EnumerateArray())
                            {
                                profile.RecentlyPlayedTracks.Add(new ProfileViewModel.RecentlyPlayedTrack
                                {
                                    Id = track.GetProperty("id").GetInt32(),
                                    SongTitle = track.GetProperty("title").GetString() ?? "Unknown",
                                    ArtistName = track.GetProperty("artist").GetString() ?? "Unknown Artist",
                                    AlbumName = "Unknown Album", // Not in current response
                                    Duration = track.TryGetProperty("duration", out var durationProp) && 
                                              durationProp.ValueKind != JsonValueKind.Null 
                                              ? TimeSpan.FromTicks(durationProp.GetInt64()).ToString(@"m\:ss") 
                                              : "--:--",
                                    CoverImageUrl = "/images/default-cover.png" // Default for now
                                });
                            }
                        }
                        
                        // Populate activity feed
                        if (statsObj.TryGetProperty("recentActivity", out JsonElement activityElement) && 
                            activityElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var activity in activityElement.EnumerateArray())
                            {
                                profile.ActivityFeedItems.Add(new ProfileViewModel.ActivityFeedItem
                                {
                                    Id = activity.GetProperty("id").GetInt32(),
                                    Type = "song_played",
                                    Description = $"Played \"{activity.GetProperty("songTitle").GetString()}\" by {activity.GetProperty("artistName").GetString()}",
                                    Timestamp = DateTime.Parse(activity.GetProperty("timestamp").GetString() ?? DateTime.UtcNow.ToString())
                                });
                            }
                        }
                    }
                    
                    // Get top artists (now dynamic!)
                    var topArtistsResponse = await SafeApiCall(
                        async () => await _apiService.GetAsync<object[]>("api/Users/top-artists"),
                        new object[0],
                        "Unable to load top artists",
                        "AccountController.Dashboard - Loading top artists"
                    );

                    if (topArtistsResponse != null && topArtistsResponse.Length > 0)
                    {
                        foreach (var artist in topArtistsResponse)
                        {
                            var artistJson = JsonSerializer.Serialize(artist);
                            var artistObj = JsonSerializer.Deserialize<JsonElement>(artistJson);
                            profile.TopArtists.Add(new ProfileViewModel.TopArtist
                            {
                                Id = artistObj.GetProperty("id").GetInt32(),
                                ArtistName = artistObj.GetProperty("name").GetString() ?? "Unknown Artist",
                                PlayCount = artistObj.GetProperty("playCount").GetInt32(),
                                ArtistImageUrl = artistObj.TryGetProperty("imageUrl", out JsonElement imageUrl) && 
                                               imageUrl.ValueKind != JsonValueKind.Null 
                                               ? imageUrl.GetString()
                                               : "/images/default-artist.png"
                            });
                        }
                    }
                }
                else
                {
                    // No user data found, add sample data
                    profile.AddSampleData();
                }
                
                return View(profile);
            },
            () => {
                // Return a default profile to avoid null reference
                var profile = new ProfileViewModel();
                profile.AddSampleData();
                SetErrorMessage("Unable to load dashboard data. Please try again later.");
                return View(profile);
            },
            "Unable to load dashboard data. Please try again later.",
            "AccountController.Dashboard");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", new { returnUrl = Url.Action("Profile") });
            }

            return await SafeApiAction(async () =>
            {
                // Get user profile from the API
                var userProfile = await SafeApiCall(
                    async () => await _apiService.GetAsync<UserDto>("api/Users/profile"),
                    (UserDto)null,
                    "Unable to load user profile",
                    "AccountController.Profile - Loading user profile"
                );
                
                // Create and populate ProfileViewModel
                var profile = new ProfileViewModel();
                
                if (userProfile != null)
                {
                    // Create a User object from UserDto
                    profile.User = new User
                    {
                        Id = userProfile.Id,
                        Username = userProfile.Username,
                        Email = userProfile.Email,
                        FirstName = userProfile.FirstName, 
                        LastName = userProfile.LastName,
                        ProfileImageUrl = userProfile.ProfileImageUrl,
                        CreatedAt = userProfile.CreatedAt
                    };
                }
                else
                {
                    // No user data found, add sample data
                    profile.AddSampleData();
                }
                
                return View(profile);
            },
            () => {
                // Return a default profile to avoid null reference
                var profile = new ProfileViewModel();
                profile.AddSampleData();
                SetErrorMessage("Unable to load profile data. Please try again later.");
                return View(profile);
            },
            "Unable to load profile data. Please try again later.",
            "AccountController.Profile");
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult AdminDashboard()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ListeningHistory(int page = 1, int pageSize = 20)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", new { returnUrl = Url.Action("ListeningHistory") });
            }

            return await SafeApiAction(async () =>
            {
                // Get listening history from API
                var historyResponse = await SafeApiCall(
                    async () => await _apiService.GetAsync<object>($"api/Users/listening-history?page={page}&pageSize={pageSize}"),
                    (object)null,
                    "Unable to load listening history at this time",
                    "AccountController.ListeningHistory - Loading history"
                );

                var viewModel = new ListeningHistoryViewModel
                {
                    CurrentPage = page,
                    PageSize = pageSize
                };

                if (historyResponse != null)
                {
                    // Convert to JsonElement for safe property access
                    var historyJson = JsonSerializer.Serialize(historyResponse);
                    var historyObj = JsonSerializer.Deserialize<JsonElement>(historyJson);

                    if (historyObj.TryGetProperty("data", out JsonElement dataElement) && 
                        dataElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in dataElement.EnumerateArray())
                        {
                            var playItem = new ListeningHistoryViewModel.ListeningHistoryItem();
                            
                            if (item.TryGetProperty("song", out JsonElement songElement))
                            {
                                playItem.Song = new ListeningHistoryViewModel.SongInfo
                                {
                                    Id = songElement.GetProperty("id").GetInt32(),
                                    Title = songElement.GetProperty("title").GetString() ?? "Unknown",
                                    Artist = songElement.GetProperty("artist").GetString() ?? "Unknown Artist",
                                    Album = songElement.TryGetProperty("album", out var albumProp) && 
                                           albumProp.ValueKind != JsonValueKind.Null 
                                           ? albumProp.GetString() : null,
                                    Genre = songElement.TryGetProperty("genre", out var genreProp) && 
                                           genreProp.ValueKind != JsonValueKind.Null 
                                           ? genreProp.GetString() : null,
                                    CoverImageUrl = songElement.TryGetProperty("coverImageUrl", out var coverProp) && 
                                                   coverProp.ValueKind != JsonValueKind.Null 
                                                   ? coverProp.GetString() : "/images/default-cover.png"
                                };

                                if (songElement.TryGetProperty("duration", out var durationProp) && 
                                    durationProp.ValueKind != JsonValueKind.Null)
                                {
                                    var durationString = durationProp.GetString();
                                    if (TimeSpan.TryParse(durationString, out var duration))
                                    {
                                        playItem.Song.Duration = duration;
                                    }
                                }
                            }

                            if (item.TryGetProperty("playedAt", out var playedAtProp))
                            {
                                playItem.PlayedAt = DateTime.Parse(playedAtProp.GetString() ?? DateTime.UtcNow.ToString());
                            }

                            if (item.TryGetProperty("listenDuration", out var listenDurationProp) && 
                                listenDurationProp.ValueKind != JsonValueKind.Null)
                            {
                                var listenDurationString = listenDurationProp.GetString();
                                if (TimeSpan.TryParse(listenDurationString, out var listenDuration))
                                {
                                    playItem.ListenDuration = listenDuration;
                                }
                            }

                            viewModel.History.Add(playItem);
                        }
                    }

                    // Set pagination info
                    if (historyObj.TryGetProperty("totalPages", out var totalPagesElement))
                    {
                        viewModel.TotalPages = totalPagesElement.GetInt32();
                    }
                    
                    if (historyObj.TryGetProperty("totalCount", out var totalCountElement))
                    {
                        viewModel.TotalCount = totalCountElement.GetInt32();
                    }
                }

                return View(viewModel);
            }, () => {
                var viewModel = new ListeningHistoryViewModel 
                { 
                    CurrentPage = page, 
                    PageSize = pageSize 
                };
                SetErrorMessage("Unable to load listening history. Please try again later.");
                return View(viewModel);
            },
            "Unable to load listening history. Please try again later.",
            "AccountController.ListeningHistory");
        }

        // Diagnostic method to test database and authentication
        [HttpGet]
        public async Task<IActionResult> Debug()
        {
            try
            {
                // Check if roles exist, create them if not
                if (!await _context.Roles.AnyAsync())
                {
                    _context.Roles.Add(new Models.Role { Id = 1, Name = "Admin", Description = "Administrator role" });
                    _context.Roles.Add(new Models.Role { Id = 2, Name = "User", Description = "Regular user role" });
                    await _context.SaveChangesAsync();
                }
                
            // Clear existing users
            var existingUsers = await _context.Users.ToListAsync();
            _context.Users.RemoveRange(existingUsers);
            await _context.SaveChangesAsync();

            // Add a test admin user directly
            var testUser = new User
            {
                Id = 999,
                FirstName = "Test",
                LastName = "Admin",
                    Username = "prabe.sh",
                    Email = "hello@prabe.sh",
                    PasswordHash = "prabesh",
                ProfileImageUrl = "https://picsum.photos/200/200?random=99",
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(testUser);
            await _context.SaveChangesAsync();

            // Add admin role for this user
            var userRole = new UserRole
            {
                UserId = testUser.Id,
                RoleId = 1 // Admin role
            };
            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();

            // Check if user was saved properly
            var allUsers = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ToListAsync();

            string result = $"Total users: {allUsers.Count}\n";
            foreach (var user in allUsers)
            {
                result += $"User: {user.Username}, Email: {user.Email}, Password: {user.PasswordHash}\n";
                result += $"Roles: {string.Join(", ", user.UserRoles?.Select(ur => ur.Role?.Name ?? "Unknown") ?? new[] { "None" })}\n";
            }

                // Log in the test user automatically
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, testUser.Username),
                    new Claim(ClaimTypes.Email, testUser.Email),
                    new Claim("UserId", testUser.Id.ToString()),
                    new Claim(ClaimTypes.Role, "Admin") // Add role claim
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                result += "\nUser automatically logged in. You can now refresh the page to see the authenticated state.";

            return Content(result, "text/plain");
            }
            catch (Exception ex)
            {
                return Content($"Error during debug: {ex.Message}\n{ex.StackTrace}", "text/plain");
            }
        }

        private IActionResult RedirectToLoginWithError()
        {
            return RedirectToAction("Login", "Account", new { error = string.Empty });
        }
    }
}
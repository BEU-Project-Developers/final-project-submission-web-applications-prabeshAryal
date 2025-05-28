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
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthService _authService;
        private readonly ApiService _apiService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(ApplicationDbContext context, AuthService authService, ApiService apiService, ILogger<AccountController> logger)
        {
            _context = context;
            _authService = authService;
            _apiService = apiService;
            _logger = logger;
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

            try
            {
                _logger.LogInformation("Attempting login for email: {Email}", model.Email);
                
                var success = await _authService.LoginAsync(model.Email, model.Password, model.RememberMe);

                _logger.LogInformation("Login attempt result: {Success}", success);

                if (success)
                {
                    // If it's an AJAX request, return JSON
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                        _logger.LogInformation("Returning AJAX success response");
                        return Json(new { success = true, redirectUrl = Url.Action("Index", "Home") });
                    }

                    _logger.LogInformation("Redirecting to home page");
                    return RedirectToAction("Index", "Home");
                }

                _logger.LogWarning("Login failed - invalid credentials for email: {Email}", model.Email);
                // If we get here, the login failed
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Invalid email or password. Please try again." });
                }

                ModelState.AddModelError(string.Empty, "Invalid email or password");
                    return View(model);
                }
            catch (HttpRequestException ex)
            {
                // Extract the specific error message
                var errorMessage = ex.Message;
                if (errorMessage.Contains(":"))
                {
                    errorMessage = errorMessage.Substring(errorMessage.IndexOf(":") + 1).Trim();
                }
                else
                {
                    errorMessage = "Invalid email or password. Please try again.";
                }
                
                _logger.LogError("Login failed with HTTP error: {ErrorMessage}", errorMessage);
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = errorMessage });
                }
                
                ModelState.AddModelError(string.Empty, errorMessage);
                return View(model);
            }
            catch (Exception ex)
            {
                // Log the error
                _logger.LogError(ex, "Login error: {ErrorMessage}", ex.Message);
                
                // If it's an AJAX request, return JSON with error
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "An error occurred during login. Please try again." });
                }

                ModelState.AddModelError(string.Empty, "An error occurred during login");
            return View(model);
            }
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
            if (ModelState.IsValid)
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
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Registration failed. Email or username may already be in use." });
                }
                
                ModelState.AddModelError(string.Empty, "Registration failed. Email or username may already be in use.");
            }

            // If we got this far, something failed
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Invalid registration attempt." });
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
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

            try
            {
                // Try to get user profile from the API
                var userProfile = await _apiService.GetAsync<UserDto>("api/Users/profile");
                
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
                    
                    // Get recently played tracks
                    try 
                    {
                        var recentTracks = await _apiService.GetAsync<List<SongDto>>("api/Users/recently-played");
                        if (recentTracks != null && recentTracks.Any())
                        {
                            foreach (var track in recentTracks)
                            {
                                profile.RecentlyPlayedTracks.Add(new ProfileViewModel.RecentlyPlayedTrack                                {
                                    Id = track.Id,
                                    SongTitle = track.Title,
                                    ArtistName = track.ArtistName,
                                    AlbumName = track.AlbumTitle,
                                    Duration = track.Duration?.ToString(@"m\:ss") ?? "--:--",
                                    CoverImageUrl = track.CoverImageUrl
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error fetching recently played: {ErrorMessage}", ex.Message);
                    }
                    
                    // Get top artists
                    try 
                    {
                        var topArtists = await _apiService.GetAsync<List<ArtistDto>>("api/Users/top-artists");
                        if (topArtists != null && topArtists.Any())
                        {
                            foreach (var artist in topArtists)
                            {
                                profile.TopArtists.Add(new ProfileViewModel.TopArtist
                    {
                                    Id = artist.Id,
                                    ArtistName = artist.Name,
                                    PlayCount = artist.MonthlyListeners,
                                    ArtistImageUrl = artist.ImageUrl
                                });
                            }
                            
                            // Set favorite artist
                            profile.FavoriteArtist = topArtists.FirstOrDefault()?.Name ?? "None";
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error fetching top artists: {ErrorMessage}", ex.Message);
                    }
                }
                else
                {
                    // No user data found, add sample data
                    profile.AddSampleData();
                }
                
                return View(profile);
            }
            catch (Exception ex)
            {
                // Log the error
                _logger.LogError(ex, "Error retrieving profile: {ErrorMessage}", ex.Message);
                
                // Return a default profile to avoid null reference
                var profile = new ProfileViewModel();
                profile.AddSampleData();
                return View(profile);
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", new { returnUrl = Url.Action("Profile") });
            }

            try
            {
                // Get user profile from the API
                var userProfile = await _apiService.GetAsync<UserDto>("api/Users/profile");
                
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
            }
            catch (Exception ex)
            {
                // Log the error
                _logger.LogError(ex, "Error retrieving profile in Profile action: {ErrorMessage}", ex.Message);
                
                // Return a default profile to avoid null reference
                var profile = new ProfileViewModel();
                profile.AddSampleData();
                return View(profile);
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult AdminDashboard()
        {
            return View();
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
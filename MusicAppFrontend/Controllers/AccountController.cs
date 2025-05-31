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
using Microsoft.AspNetCore.Http; 
using MusicApp.Models.DTOs; // For UserDto (frontend) and ApiResponse
using MusicApp.Models; // For MusicApp.Models.User

namespace MusicApp.Controllers
{
    public class AccountController : BaseAppController
    {
        private readonly ApplicationDbContext _context;
        private readonly FileUploadService _fileUploadService;
        private readonly IConfiguration _configuration;

        public AccountController(
            ApplicationDbContext context,
            AuthService authService,
            ApiService apiService,
            FileUploadService fileUploadService,
            IConfiguration configuration,
            ILogger<AccountController> logger)
            : base(apiService, authService, logger)
        {
            _context = context; // Not used in provided snippet, but keeping
            _fileUploadService = fileUploadService;
            _configuration = configuration;
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
                return RedirectToAction("Login", "Account");
            }

            var authUserDto = await _authService.GetCurrentUserAsync();
            if (authUserDto == null)
            {
                _logger.LogWarning("Dashboard: User is authenticated but GetCurrentUserAsync returned null.");
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Account");
            }

            // Map AuthUserDto to MusicApp.Models.User
            var feUserModel = new MusicApp.Models.User
            {
                Id = authUserDto.Id,
                Username = authUserDto.Username,
                Email = authUserDto.Email,
                FirstName = authUserDto.FirstName,
                LastName = authUserDto.LastName,
                ProfileImageUrl = authUserDto.ProfileImageUrl,
                Bio = authUserDto.Bio,
            };
            
            var backendUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5117";
            var model = new ProfileViewModel();
            model.InitializeFromUser(feUserModel, backendUrl);

            return View(model);
        }


        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var authUserDto = await _authService.GetCurrentUserAsync();
            if (authUserDto == null)
            {
                _logger.LogWarning("Profile GET: User not authenticated or could not be fetched.");
                return Challenge(); 
            }
            
            // Map AuthUserDto to MusicApp.Models.User
            var feUserModel = new MusicApp.Models.User
            {
                Id = authUserDto.Id,
                Username = authUserDto.Username,
                Email = authUserDto.Email,
                FirstName = authUserDto.FirstName,
                LastName = authUserDto.LastName,
                ProfileImageUrl = authUserDto.ProfileImageUrl,
                Bio = authUserDto.Bio
            };

            var backendUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5117";
            var model = new ProfileViewModel();
            model.InitializeFromUser(feUserModel, backendUrl);

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var authUserDto = await _authService.GetCurrentUserAsync();
            if (authUserDto == null)
            {
                _logger.LogWarning("Profile POST: Current user not found, though authorized. Challenging.");
                return Challenge(); 
            }

            // Map AuthUserDto to MusicApp.Models.User for re-populating view model if needed
            var currentUserFeModel = new MusicApp.Models.User
            {
                Id = authUserDto.Id,
                Username = authUserDto.Username,
                Email = authUserDto.Email,
                FirstName = authUserDto.FirstName,
                LastName = authUserDto.LastName,
                ProfileImageUrl = authUserDto.ProfileImageUrl,
                Bio = authUserDto.Bio
            };

            var backendUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5117";
            model.Email = authUserDto.Email; // Email is not editable but good to have in VM
            // Re-initialize ProfileImageUrl and User for the view model in case of error return
            var tempViewModelForDisplay = new ProfileViewModel();
            tempViewModelForDisplay.InitializeFromUser(currentUserFeModel, backendUrl);
            model.ProfileImageUrl = tempViewModelForDisplay.ProfileImageUrl;
            model.User = currentUserFeModel; 

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Profile POST: Model state is invalid for user {Username}.", authUserDto.Username);
                return View(model); // Return with validation errors
            }

            string? newProfileImageUrl = authUserDto.ProfileImageUrl; // Start with existing image URL

            if (model.ProfileImage != null && model.ProfileImage.Length > 0)
            {
                _logger.LogInformation("Profile POST: New profile image uploaded by user {Username}: {FileName}, Size: {Length} bytes.", authUserDto.Username, model.ProfileImage.FileName, model.ProfileImage.Length);
                
                // Assuming UploadProfileImageAsync returns string? (path on success, error message or null on failure)
                // Based on compiler errors: CS1061 'string' does not contain 'Success', 'FilePath', 'ErrorMessage'
                string? uploadedFilePathOrError = await _fileUploadService.UploadProfileImageAsync(model.ProfileImage);

                if (!string.IsNullOrEmpty(uploadedFilePathOrError) && 
                    !uploadedFilePathOrError.StartsWith("Error:", StringComparison.OrdinalIgnoreCase) && 
                    !uploadedFilePathOrError.StartsWith("Failed:", StringComparison.OrdinalIgnoreCase)) // Common error prefixes
                {
                    newProfileImageUrl = uploadedFilePathOrError; // This should be the relative path like /uploads/image.jpg
                    _logger.LogInformation("Profile POST: Profile image for user {Username} uploaded successfully. New path: {FilePath}", authUserDto.Username, newProfileImageUrl);
                }
                else
                {
                    string errorMessage = "Image upload failed. Please try another image.";
                    if (!string.IsNullOrEmpty(uploadedFilePathOrError)) {
                        // Attempt to extract a cleaner error message if prefixed
                        if (uploadedFilePathOrError.StartsWith("Error:", StringComparison.OrdinalIgnoreCase)) errorMessage = uploadedFilePathOrError.Substring(6).Trim();
                        else if (uploadedFilePathOrError.StartsWith("Failed:", StringComparison.OrdinalIgnoreCase)) errorMessage = uploadedFilePathOrError.Substring(7).Trim();
                        else errorMessage = uploadedFilePathOrError; // Use as is if no known prefix
                    }
                    _logger.LogError("Profile POST: Profile image upload failed for user {Username}. Details: {ErrorMessage}", authUserDto.Username, uploadedFilePathOrError ?? "Unknown error from FileUploadService.");
                    ModelState.AddModelError("ProfileImage", errorMessage);
                    return View(model);
                }
            }

            string firstName = authUserDto.FirstName ?? ""; 
            string lastName = authUserDto.LastName ?? "";   

            if (!string.IsNullOrWhiteSpace(model.FullName)) {
                var nameParts = model.FullName.Trim().Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                firstName = nameParts.Length > 0 ? nameParts[0] : "";
                lastName = nameParts.Length > 1 ? nameParts[1] : "";
            }
            
            var updateUserPayload = new 
            {
                FirstName = firstName,
                LastName = lastName,
                Username = model.Username, 
                Bio = model.Bio,
                ProfileImageUrl = newProfileImageUrl 
            };

            _logger.LogInformation("Profile POST: Attempting to update profile for user ID {UserId} ({Username})", authUserDto.Id, authUserDto.Username);

            try
            {
                var apiResponse = await _apiService.PutAsync<ApiResponse<UserDto>>($"users/{authUserDto.Id}", updateUserPayload);

                if (apiResponse != null && apiResponse.Success && apiResponse.Data != null) 
                {
                    _logger.LogInformation("Profile POST: Profile updated successfully via API for user ID {UserId} ({Username}).", authUserDto.Id, authUserDto.Username);
                    
                    // Call the new method in AuthService to update claims
                    await _authService.UpdateUserClaimsAsync(apiResponse.Data);
                    
                    TempData["SuccessMessage"] = "Profile updated successfully!";
                    return RedirectToAction("Dashboard");
                }
                else
                {
                    _logger.LogError("Profile POST: API call to update profile failed for user ID {UserId} ({Username}). API Message: {ApiMessage}", authUserDto.Id, authUserDto.Username, apiResponse?.Message ?? "No message from API.");
                    ModelState.AddModelError(string.Empty, apiResponse?.Message ?? "Failed to update profile. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Profile POST: Exception during profile update for user ID {UserId} ({Username}).", authUserDto.Id, authUserDto.Username);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while updating your profile. Please try again later.");
            }

            return View(model);
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
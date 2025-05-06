using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MusicApp.Models;
using MusicApp.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MusicApp.Data;

namespace MusicApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Dashboard");
            }
            
            // Instead of showing the Login view, redirect to Home with a query param to open auth modal
            TempData["OpenAuthModal"] = "login";
            if (!string.IsNullOrEmpty(returnUrl))
            {
                TempData["ReturnUrl"] = returnUrl;
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            // Handle RememberMe explicitly to avoid "on" value parsing error
            if (Request.Form.TryGetValue("RememberMe", out var rememberMeValue))
            {
                model.RememberMe = rememberMeValue == "true";
            }

            if (ModelState.IsValid)
            {
                // First check if any users exist
                var allUsers = await _context.Users.ToListAsync();
                if (allUsers.Count == 0)
                {
                    ModelState.AddModelError(string.Empty, "No users found in database. Please register first.");
                    
                    // Redirect to home with error message
                    TempData["AuthError"] = "No users found in database. Please register first.";
                    TempData["OpenAuthModal"] = "register";
                    return RedirectToAction("Index", "Home");
                }

                var user = await _context.Users
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "User with this email not found.");
                    
                    // Redirect to home with error message
                    TempData["AuthError"] = "User with this email not found.";
                    TempData["OpenAuthModal"] = "login";
                    if (!string.IsNullOrEmpty(returnUrl))
                    {
                        TempData["ReturnUrl"] = returnUrl;
                    }
                    return RedirectToAction("Index", "Home");
                }

                if (user.PasswordHash != model.Password)
                {
                    ModelState.AddModelError(string.Empty, "Password does not match.");
                    
                    // Redirect to home with error message
                    TempData["AuthError"] = "Password does not match.";
                    TempData["OpenAuthModal"] = "login";
                    if (!string.IsNullOrEmpty(returnUrl))
                    {
                        TempData["ReturnUrl"] = returnUrl;
                    }
                    return RedirectToAction("Index", "Home");
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim("UserId", user.Id.ToString())
                };

                foreach (var userRole in user.UserRoles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, userRole.Role.Name));
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // Redirect to returnUrl if provided and valid, otherwise to Dashboard
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                else
                    return RedirectToAction("Dashboard");
            }

            // If model validation fails, redirect to home with auth modal open
            TempData["OpenAuthModal"] = "login";
            if (!string.IsNullOrEmpty(returnUrl))
            {
                TempData["ReturnUrl"] = returnUrl;
            }
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                TempData["AuthError"] = error.ErrorMessage;
                break; // Just get the first error
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Dashboard");
            }
            
            // Instead of showing the Register view, redirect to Home with a query param to open auth modal
            TempData["OpenAuthModal"] = "register";
            if (!string.IsNullOrEmpty(returnUrl))
            {
                TempData["ReturnUrl"] = returnUrl;
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email already in use.");
                    
                    // Redirect to home with error message
                    TempData["AuthError"] = "Email already in use.";
                    TempData["OpenAuthModal"] = "register";
                    if (!string.IsNullOrEmpty(returnUrl))
                    {
                        TempData["ReturnUrl"] = returnUrl;
                    }
                    return RedirectToAction("Index", "Home");
                }

                if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                {
                    ModelState.AddModelError("Username", "Username already taken.");
                    
                    // Redirect to home with error message
                    TempData["AuthError"] = "Username already taken.";
                    TempData["OpenAuthModal"] = "register";
                    if (!string.IsNullOrEmpty(returnUrl))
                    {
                        TempData["ReturnUrl"] = returnUrl;
                    }
                    return RedirectToAction("Index", "Home");
                }

                var user = new User
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Username = model.Username,
                    Email = model.Email,
                    PasswordHash = model.Password, // In real app, use proper password hashing
                    ProfileImageUrl = $"https://picsum.photos/200/200?random={DateTime.Now.Ticks}",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Assign default role
                var userRole = new UserRole
                {
                    UserId = user.Id,
                    RoleId = 2 // Regular user role
                };
                _context.UserRoles.Add(userRole);
                await _context.SaveChangesAsync();

                // Log the user in immediately
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim("UserId", user.Id.ToString()),
                    new Claim(ClaimTypes.Role, "User") // Default role
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties();

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // Redirect to returnUrl if provided and valid, otherwise to Dashboard
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                else
                    return RedirectToAction("Dashboard");
            }

            // If model validation fails, redirect to home with auth modal open
            TempData["OpenAuthModal"] = "register";
            if (!string.IsNullOrEmpty(returnUrl))
            {
                TempData["ReturnUrl"] = returnUrl;
            }
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                TempData["AuthError"] = error.ErrorMessage;
                break; // Just get the first error
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }

            var userId = int.Parse(User.FindFirst("UserId").Value);
            var user = await _context.Users
                .Include(u => u.Playlists)
                .Include(u => u.Followers)
                .Include(u => u.Following)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound();
            }

            var viewModel = new ViewModels.ProfileViewModel
            {
                User = user,
                RecentlyPlayedTracks = new List<ProfileViewModel.RecentlyPlayedTrack>
                {
                    new ProfileViewModel.RecentlyPlayedTrack
                    {
                        Id = 1,
                        SongTitle = "Sample Track 1",
                        ArtistName = "Sample Artist",
                        AlbumName = "Sample Album",
                        Duration = "3:45",
                        CoverImageUrl = "https://picsum.photos/200/200?random=1"
                    }
                },
                TopArtists = new List<ProfileViewModel.TopArtist>
                {
                    new ProfileViewModel.TopArtist
                    {
                        Id = 1,
                        ArtistName = "Sample Artist",
                        PlayCount = 150,
                        ArtistImageUrl = "https://picsum.photos/200/200?random=2"
                    }
                },
                ActivityFeedItems = new List<ProfileViewModel.ActivityFeedItem>
                {
                    new ProfileViewModel.ActivityFeedItem
                    {
                        Id = 1,
                        Type = "playlist_created",
                        Description = "Created a new playlist",
                        Timestamp = DateTime.UtcNow
                    }
                }
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }

            var userId = int.Parse(User.FindFirst("UserId").Value);
            var user = await _context.Users
                .Include(u => u.Playlists)
                .Include(u => u.Followers)
                .Include(u => u.Following)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound();
            }

            var viewModel = new ViewModels.ProfileViewModel
            {
                User = user,
                RecentlyPlayedTracks = new List<ProfileViewModel.RecentlyPlayedTrack>
                {
                    new ProfileViewModel.RecentlyPlayedTrack
                    {
                        Id = 1,
                        SongTitle = "Sample Track 1",
                        ArtistName = "Sample Artist",
                        AlbumName = "Sample Album",
                        Duration = "3:45",
                        CoverImageUrl = "https://picsum.photos/200/200?random=3"
                    }
                },
                TopArtists = new List<ProfileViewModel.TopArtist>
                {
                    new ProfileViewModel.TopArtist
                    {
                        Id = 1,
                        ArtistName = "Sample Artist",
                        PlayCount = 150,
                        ArtistImageUrl = "https://picsum.photos/200/200?random=4"
                    }
                },
                ActivityFeedItems = new List<ProfileViewModel.ActivityFeedItem>
                {
                    new ProfileViewModel.ActivityFeedItem
                    {
                        Id = 1,
                        Type = "playlist_created",
                        Description = "Created a new playlist",
                        Timestamp = DateTime.UtcNow
                    }
                }
            };

            return View(viewModel);
        }

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
    }
} 
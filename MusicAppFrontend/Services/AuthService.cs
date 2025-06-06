using Microsoft.JSInterop;
using MusicApp.ViewModels;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace MusicApp.Services
{    public class AuthService
    {
        private readonly ApiService _apiService;
        private readonly IJSRuntime _jsRuntime;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuthService> _logger;
        private static bool _isServerSideRendering;

        public AuthService(ApiService apiService, IJSRuntime jsRuntime, IHttpContextAccessor httpContextAccessor, ILogger<AuthService> logger)
        {            _apiService = apiService;
            _jsRuntime = jsRuntime;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            
            // Check if we're in a server-side rendering context
            _isServerSideRendering = jsRuntime is IJSInProcessRuntime == false;
        }

        private async Task<bool> SetLocalStorageAsync(string key, string value)
        {
            if (_isServerSideRendering)
                return false;
                
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        private async Task<string> GetLocalStorageAsync(string key)
        {
            if (_isServerSideRendering)
                return null;
                
            try
            {
                return await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
            }
            catch
            {
                return null;
            }
        }
        
        private async Task RemoveLocalStorageAsync(string key)
        {
            if (_isServerSideRendering)
                return;
                
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
            }
            catch
            {
                // Ignore errors during removal
            }
        }

        public async Task<bool> LoginAsync(string usernameOrEmail, string password, bool rememberMe)
        {
            try
            {
                var response = await _apiService.PostAsync<LoginResponse>(
                    "api/Auth/login", 
                    new { usernameOrEmail, password, rememberMe });
                
                if (response != null && response.User != null)
                {
                    // Store tokens in local storage for API requests
                    await SetLocalStorageAsync("jwt_token", response.Token);
                    await SetLocalStorageAsync("refresh_token", response.RefreshToken);
                    await SetLocalStorageAsync("user_info", JsonSerializer.Serialize(response.User));
                      // Also set up cookie authentication for server-side validation
                    var user = response.User;
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim("FirstName", user.FirstName ?? string.Empty),
                        new Claim("LastName", user.LastName ?? string.Empty),
                        new Claim("ProfileImageUrl", user.ProfileImageUrl ?? string.Empty),
                        new Claim("Bio", user.Bio ?? string.Empty),
                        new Claim("jwt_token", response.Token)  // Store JWT token in claims
                    };
                    
                    // Add roles as claims
                    if (user.Roles != null)
                    {
                        foreach (var role in user.Roles)
                        {
                            claims.Add(new Claim(ClaimTypes.Role, role));
                        }
                    }
                    
                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = rememberMe,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7), // Set cookie expiration
                        AllowRefresh = true
                    };
                    
                    // Make sure we have a valid HttpContext
                    if (_httpContextAccessor.HttpContext != null)
                    {
                        // Sign in using cookie authentication
                        await _httpContextAccessor.HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(claimsIdentity),
                            authProperties);
                            
                        _logger.LogInformation("User authenticated via cookies: {IsAuthenticated}", _httpContextAccessor.HttpContext.User.Identity.IsAuthenticated);
                    }
                    else
                    {
                        _logger.LogWarning("HttpContext is null during login attempt");
                    }
                    
                    return true;
                }
                return false;
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("401"))
            {
                _logger.LogError(ex, "Login failed with authentication error: {ErrorMessage}", ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error: {ErrorMessage}", ex.Message);
                return false;
            }
        }

        public async Task<bool> RegisterAsync(RegisterViewModel model)
        {
            try
            {
                _logger.LogInformation("Attempting to register user: {Username}", model.Username);
                
                // Use LoginResponse to parse the backend response that now includes tokens
                var response = await _apiService.PostAsync<LoginResponse>("api/Auth/register", model);
                
                if (response == null)
                {
                    _logger.LogWarning("Registration failed: null response received");
                    return false;
                }
                
                _logger.LogInformation("Registration response received with user: {Username}", 
                    response.User?.Username ?? "null");
                
                // Check if we got tokens and user data back
                if (response.User != null && !string.IsNullOrEmpty(response.Token))
                {
                    _logger.LogInformation("Registration successful with token for: {Username}", response.User.Username);
                    
                    // Store tokens in local storage for API requests
                    await SetLocalStorageAsync("jwt_token", response.Token);
                    await SetLocalStorageAsync("refresh_token", response.RefreshToken);
                    await SetLocalStorageAsync("user_info", JsonSerializer.Serialize(response.User));
                      // Set up cookie authentication for server-side validation
                    var user = response.User;
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim("FirstName", user.FirstName ?? string.Empty),
                        new Claim("LastName", user.LastName ?? string.Empty),
                        new Claim("ProfileImageUrl", user.ProfileImageUrl ?? string.Empty),
                        new Claim("Bio", user.Bio ?? string.Empty),
                        new Claim("jwt_token", response.Token)  // Store JWT token in claims
                    };
                    
                    // Add roles as claims
                    if (user.Roles != null)
                    {
                        foreach (var role in user.Roles)
                        {
                            claims.Add(new Claim(ClaimTypes.Role, role));
                        }
                    }
                    
                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true, // Remember me by default for new registrations
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7), // Set cookie expiration
                        AllowRefresh = true
                    };
                    
                    // Make sure we have a valid HttpContext
                    if (_httpContextAccessor.HttpContext != null)
                    {
                        // Sign in using cookie authentication
                        await _httpContextAccessor.HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(claimsIdentity),
                            authProperties);
                            
                        _logger.LogInformation("User registered and authenticated via cookies: {IsAuthenticated}", 
                            _httpContextAccessor.HttpContext.User.Identity.IsAuthenticated);
                    }
                    else
                    {
                        _logger.LogWarning("HttpContext is null during registration - cookie auth skipped");
                    }
                    
                    return true;
                }
                else if (response.User != null)
                {
                    _logger.LogInformation("Registration successful but without token for: {Username}", 
                        response.User.Username);
                    
                    // This is the fallback case where registration succeeded but auto-login failed
                    // We still consider registration successful, but user will need to login manually
                    
                    // Store just the user info (without tokens)
                    await SetLocalStorageAsync("user_info", JsonSerializer.Serialize(response.User));
                    
                    return true;
                }
                
                _logger.LogWarning("Registration failed: User property not found in response");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration error: {ErrorMessage}", ex.Message);
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            // Call logout endpoint to invalidate token
            try
            {
                var refreshToken = await GetLocalStorageAsync("refresh_token");
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    await _apiService.PostAsync<object>("api/Auth/revoke-token", new { token = refreshToken });
                }
            }
            catch { /* Ignore errors during logout */ }
            
            // Clear tokens in browser
            await RemoveLocalStorageAsync("jwt_token");
            await RemoveLocalStorageAsync("refresh_token");
            await RemoveLocalStorageAsync("user_info");
            
            // Sign out from cookie authentication
            await _httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
        
        public bool IsAuthenticated()
        {
            if (_httpContextAccessor.HttpContext == null)
            {
                _logger.LogWarning("HttpContext is null when checking authentication");
                return false;
            }
            
            var isAuthenticated = _httpContextAccessor.HttpContext.User?.Identity?.IsAuthenticated ?? false;
            _logger.LogInformation("IsAuthenticated check: {IsAuthenticated}", isAuthenticated);
            
            // Double check if we have the necessary claims
            if (isAuthenticated)
            {
                var nameIdentifier = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(nameIdentifier))
                {
                    _logger.LogWarning("User appears authenticated but missing NameIdentifier claim");
                    return false;
                }
            }
            
            return isAuthenticated;
        }
        
        public async Task<bool> IsAuthenticatedAsync()
        {
            // Check cookie authentication first
            if (IsAuthenticated())
            {
                return true;
            }
            
            // Fallback to JWT token check
            var token = await GetLocalStorageAsync("jwt_token");
            return !string.IsNullOrEmpty(token);
        }
        
        public async Task<AuthUserDto> GetCurrentUserAsync()
        {
            // Try to get from HttpContext claims
            if (IsAuthenticated())
            {
                try
                {
                    var user = _httpContextAccessor.HttpContext.User;
                    var id = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var username = user.FindFirst(ClaimTypes.Name)?.Value;
                    var email = user.FindFirst(ClaimTypes.Email)?.Value;
                    
                    if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(username))
                    {
                        // Get full user profile from API
                        try
                        {
                            var userProfile = await _apiService.GetAsync<AuthUserDto>("api/Users/profile");
                            if (userProfile != null)
                            {
                                return userProfile;
                            }
                        }                        catch
                        {
                            // If API call fails, return basic user info from claims
                            var firstNameClaim = user.FindFirst("FirstName")?.Value ?? string.Empty;
                            var lastNameClaim = user.FindFirst("LastName")?.Value ?? string.Empty;
                            var profileImageClaim = user.FindFirst("ProfileImageUrl")?.Value ?? string.Empty;
                            var bioClaim = user.FindFirst("Bio")?.Value ?? string.Empty;
                            
                            return new AuthUserDto
                            {
                                Id = int.Parse(id),
                                Username = username,
                                Email = email,
                                FirstName = firstNameClaim,
                                LastName = lastNameClaim,
                                ProfileImageUrl = profileImageClaim,
                                Bio = bioClaim,
                                Roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList()
                            };
                        }
                    }
                }
                catch 
                {
                    // Continue to try from local storage if any error occurs
                }
            }
            
            // Fallback to local storage
            var userJson = await GetLocalStorageAsync("user_info");
            
            if (string.IsNullOrEmpty(userJson))
                return null;
                
            try
            {
                return JsonSerializer.Deserialize<AuthUserDto>(userJson, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            catch
            {
                return null;
            }
        }

        // Fix CS8602: Dereference of a possibly null reference at line 283
        private string? GetUserEmail()
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.Name;
        }

        // Fix CS8601: Possible null reference assignment at line 306
        private async Task<string?> GetTokenAsync()
        {
            var token = await GetLocalStorageAsync("authToken");
            return token ?? null;
        }

        // Fix CS8603: Possible null reference return at lines 322, 326, 334
        public async Task<string?> GetAuthTokenAsync()
        {
            var token = await GetLocalStorageAsync("authToken");
            return token ?? null;
        }
        public async Task<string?> GetRefreshTokenAsync()
        {
            var token = await GetLocalStorageAsync("refreshToken");
            return token ?? null;
        }
        public async Task<string?> GetUserIdAsync()
        {
            var userId = await GetLocalStorageAsync("userId");
            return userId ?? null;
        }        public async Task UpdateUserClaimsAsync(AuthUserDto user)
        {
            if (_httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim("FirstName", user.FirstName),
                    new Claim("LastName", user.LastName),
                    new Claim("ProfileImageUrl", user.ProfileImageUrl ?? string.Empty),
                    new Claim("Bio", user.Bio ?? string.Empty)
                };

                // Preserve JWT token from existing claims or use updated token from HttpContext.Items
                var existingToken = _httpContextAccessor.HttpContext.User.FindFirst("jwt_token")?.Value;
                var updatedToken = _httpContextAccessor.HttpContext.Items["UpdatedJwtToken"] as string;
                var tokenToUse = !string.IsNullOrEmpty(updatedToken) ? updatedToken : existingToken;
                
                if (!string.IsNullOrEmpty(tokenToUse))
                {
                    claims.Add(new Claim("jwt_token", tokenToUse));
                    _logger.LogInformation("UpdateUserClaimsAsync: Preserving JWT token in claims (length: {TokenLength})", tokenToUse.Length);
                }
                else
                {
                    _logger.LogWarning("UpdateUserClaimsAsync: No JWT token found to preserve in claims");
                }

                if (user.Roles != null)
                {
                    foreach (var role in user.Roles)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, role));
                    }
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7),
                    AllowRefresh = true
                };

                await _httpContextAccessor.HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                _logger.LogInformation("Updated user claims for user: {Username}", user.Username);
            }
        }    }    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public AuthUserDto User { get; set; } = new AuthUserDto();
        // Removed Success and Message properties to match backend TokenResponseDTO
    }

    public class AuthUserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public string? Bio { get; set; } // Added missing Bio property
        public List<string> Roles { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
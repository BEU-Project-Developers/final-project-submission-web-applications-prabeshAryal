using MusicApp.ViewModels;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace MusicApp.Services
{    public class AuthService
    {
        private readonly ApiService _apiService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuthService> _logger;

        public AuthService(ApiService apiService, IHttpContextAccessor httpContextAccessor, ILogger<AuthService> logger)
        {
            _apiService = apiService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }public async Task<bool> LoginAsync(string email, string password, bool rememberMe)
        {
            try
            {
                var loginData = new { UserIdentifier = email, Password = password };
                var response = await _apiService.PostAsync<LoginResponse>("api/auth/login", loginData);                if (response?.Token == null)
                {
                    return false;
                }

                // Create the claims identity
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, response.User.Id.ToString()),
                    new Claim(ClaimTypes.Name, response.User.Username),
                    new Claim(ClaimTypes.Email, response.User.Email)
                };

                // Store tokens as claims for easy access
                claims.Add(new Claim("AccessToken", response.Token));
                if (!string.IsNullOrEmpty(response.RefreshToken))
                {
                    claims.Add(new Claim("RefreshToken", response.RefreshToken));
                }

                // Add roles if any
                if (response.User.Roles != null)
                {
                    foreach (var role in response.User.Roles)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, role));
                    }
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = rememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                };

                await _httpContextAccessor.HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user {Email}", email);
                return false;
            }
        }

        public async Task<bool> RegisterAsync(RegisterViewModel model)
        {
            try
            {
                var registerData = new
                {
                    Username = model.Username,
                    Email = model.Email,
                    Password = model.Password,
                    ConfirmPassword = model.ConfirmPassword,
                    FirstName = model.FirstName,
                    LastName = model.LastName
                };

                var response = await _apiService.PostAsync<dynamic>("api/auth/register", registerData);
                return response != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for user {Email}", model.Email);
                return false;
            }
        }        public async Task LogoutAsync()
        {
            try
            {
                await _httpContextAccessor.HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                throw;
            }
        }

        public bool IsAuthenticated()
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
        }
    }

    public class LoginResponse
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public AuthUserDto User { get; set; }
    }

    public class AuthUserDto
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string ProfileImageUrl { get; set; }
        public List<string> Roles { get; set; }
    }
}
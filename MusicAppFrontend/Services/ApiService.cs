using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Http;

namespace MusicApp.Services
{
    public class ApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IJSRuntime _jsRuntime;
        private readonly IConfiguration _configuration;
        private readonly string _baseUrl;
        private static bool _isServerSideRendering;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApiService(
            IHttpClientFactory httpClientFactory, 
            IJSRuntime jsRuntime, 
            IConfiguration configuration, 
            IHttpContextAccessor httpContextAccessor = null)
        {
            _httpClientFactory = httpClientFactory;
            _jsRuntime = jsRuntime;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor ?? new HttpContextAccessor();
            _baseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5117";
            
            // Check if we're in a server-side rendering context
            _isServerSideRendering = jsRuntime is IJSInProcessRuntime == false;
        }

        private async Task<string?> GetTokenAsync()
        {
            // First check if we're authenticated via cookies and can get the token from HttpContext
            if (_httpContextAccessor?.HttpContext?.User?.Identity?.IsAuthenticated == true)
            {
                var tokenClaim = _httpContextAccessor.HttpContext.User.FindFirst("jwt_token");
                if (tokenClaim != null && !string.IsNullOrEmpty(tokenClaim.Value))
                {
                    Console.WriteLine("Using token from HttpContext claims");
                    return tokenClaim.Value;
                }
            }
            
            // Fallback to localStorage if not available in HttpContext
            if (_isServerSideRendering)
                return null;
                
            try
            {
                // We'll attempt to get the token from localStorage using JS interop
                var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "jwt_token");
                
                if (!string.IsNullOrEmpty(token))
                {
                    Console.WriteLine("Using token from localStorage");
                    return token;
                }
                return null;
            }
            catch (InvalidOperationException)
            {
                // This will happen during server-side rendering
                return null;
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error getting token: {ex.Message}");
                return null;
            }
        }

        public async Task<HttpClient> GetHttpClientAsync()
        {
            var client = _httpClientFactory.CreateClient("MusicApi");
            client.BaseAddress = new Uri(_baseUrl);
            
            // Add headers for cross-origin requests
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("Origin", "http://localhost:5085");

            var token = await GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }

        public async Task<T> GetAsync<T>(string endpoint)
        {
            var client = await GetHttpClientAsync();
            var response = await client.GetAsync(endpoint);
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // Handle token refresh or redirect to login
                await HandleUnauthorized();
                return default;
            }
              response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }

        public async Task<T> PostAsync<T>(string endpoint, object data)
        {
            var client = await GetHttpClientAsync();
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            Console.WriteLine($"Making POST request to {endpoint}");
            Console.WriteLine($"Request data: {json}");
            
            var response = await client.PostAsync(endpoint, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            Console.WriteLine($"Response status: {response.StatusCode}");
            Console.WriteLine($"Response content: {responseContent}");
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {                // Try to deserialize error message
                try
                {
                    var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, 
                        new JsonSerializerOptions 
                        { 
                            PropertyNameCaseInsensitive = true,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });
                    
                    if (errorResponse != null && !string.IsNullOrEmpty(errorResponse.Message))
                    {
                        throw new HttpRequestException($"401 Unauthorized: {errorResponse.Message}", null, System.Net.HttpStatusCode.Unauthorized);
                    }
                }
                catch (JsonException) { /* Continue with default handling */ }
                
                await HandleUnauthorized();
                throw new HttpRequestException("Authentication failed", null, System.Net.HttpStatusCode.Unauthorized);
            }
            
            if (!response.IsSuccessStatusCode)
            {                // Try to deserialize error message
                try
                {
                    var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, 
                        new JsonSerializerOptions 
                        { 
                            PropertyNameCaseInsensitive = true,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });
                    
                    if (errorResponse != null && !string.IsNullOrEmpty(errorResponse.Message))
                    {
                        throw new HttpRequestException($"API request failed with status code {response.StatusCode}: {errorResponse.Message}");
                    }
                }
                catch (JsonException) { /* Continue with default error message */ }
                
                throw new HttpRequestException($"API request failed with status code {response.StatusCode}: {responseContent}");
            }
            
            try
            {
                return JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JSON deserialization error: {ex.Message}");
                Console.WriteLine($"Response content: {responseContent}");
                throw;
            }
        }
        
        public async Task<T> PutAsync<T>(string endpoint, object data)
        {
            var client = await GetHttpClientAsync();
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await client.PutAsync(endpoint, content);
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await HandleUnauthorized();
                return default;
            }
              response.EnsureSuccessStatusCode();
            
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        
        public async Task DeleteAsync(string endpoint)
        {
            var client = await GetHttpClientAsync();
            var response = await client.DeleteAsync(endpoint);
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await HandleUnauthorized();
                return;
            }
            
            response.EnsureSuccessStatusCode();
        }
        
        public async Task<Stream?> GetStreamAsync(string endpoint)
        {
            var client = await GetHttpClientAsync();
            var response = await client.GetAsync(endpoint);
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await HandleUnauthorized();
                return null;
            }
            
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStreamAsync();
        }
        
        public async Task<string?> UploadFileAsync(string endpoint, IFormFile file, string paramName = "file")
        {
            var client = await GetHttpClientAsync();
            using var content = new MultipartFormDataContent();
            
            using var fileStream = file.OpenReadStream();
            using var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            
            content.Add(streamContent, paramName, file.FileName);
            
            var response = await client.PostAsync(endpoint, content);
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await HandleUnauthorized();
                return null;
            }
            
            response.EnsureSuccessStatusCode();
              var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<FileUploadResultDto>(responseContent, 
                new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                
            return result?.FilePath;
        }

        private async Task HandleUnauthorized()
        {
            if (_isServerSideRendering)
                return;
                
            // Try to refresh the token
            try
            {
                // Get refresh token from localStorage
                var refreshToken = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "refresh_token");
                
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    try
                    {
                        // Create a new HttpClient without token requirements
                        var client = _httpClientFactory.CreateClient("MusicApi");
                        client.BaseAddress = new Uri(_baseUrl);
                        
                        var content = new StringContent(
                            JsonSerializer.Serialize(new { token = refreshToken }), 
                            Encoding.UTF8, 
                            "application/json");
                        
                        var response = await client.PostAsync("api/Auth/refresh-token", content);
                        
                        if (response.IsSuccessStatusCode)
                        {
                            var responseContent = await response.Content.ReadAsStringAsync();                            var tokenResponse = JsonSerializer.Deserialize<TokenResponseDto>(
                                responseContent, 
                                new JsonSerializerOptions 
                                { 
                                    PropertyNameCaseInsensitive = true,
                                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                                });
                            
                            if (tokenResponse != null)
                            {
                                // Store the new tokens in localStorage
                                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "jwt_token", tokenResponse.Token);
                                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "refresh_token", tokenResponse.RefreshToken);
                                
                                // Successfully refreshed token, return without redirect
                                return;
                            }
                        }
                        // If we get here, token refresh failed - proceed to logout
                    }
                    catch
                    {
                        // Ignore errors during token refresh
                    }
                }
            }
            catch
            {
                // Ignore errors during token refresh in server-side rendering
            }
            
            // Token refresh failed or wasn't attempted, log the user out
            if (!_isServerSideRendering)
            {
                // Clear tokens from localStorage
                try
                {
                    await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "jwt_token");
                    await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "refresh_token");
                    await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "user_info");
                    
                    // Use HttpContextAccessor for redirect if available (works in MVC/Razor Pages),
                    // otherwise fall back to JS redirect (for Blazor components)
                    if (_httpContextAccessor?.HttpContext != null)
                    {
                        _httpContextAccessor.HttpContext.Response.Redirect("/Home/Index?authModal=login&error=Your+session+has+expired");
                    }
                    else
                    {
                        await _jsRuntime.InvokeVoidAsync("window.location.href", "/Home/Index?authModal=login&error=Your+session+has+expired");
                    }
                }
                catch
                {
                    // Ignore errors during cleanup
                }
            }
        }
    }
    
    public class FileUploadResultDto
    {
        public bool Success { get; set; }
        public string FilePath { get; set; }
        public string ErrorMessage { get; set; }
    }
    
    public class TokenResponseDto
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
    }

    public class ErrorResponse
    {
        public string Message { get; set; }
        public bool Success { get; set; }
    }
}
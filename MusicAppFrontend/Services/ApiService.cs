using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Http;

namespace MusicApp.Services
{    public class ApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IJSRuntime _jsRuntime;
        private readonly IConfiguration _configuration;
        private readonly string _baseUrl;
        private static bool _isServerSideRendering;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ApiService> _logger;

        public ApiService(
            IHttpClientFactory httpClientFactory, 
            IJSRuntime jsRuntime,            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor = null,
            ILogger<ApiService> logger = null)
        {
            _httpClientFactory = httpClientFactory;
            _jsRuntime = jsRuntime;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor ?? new HttpContextAccessor();
            _logger = logger;
            _baseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5117";
            
            // Check if we're in a server-side rendering context
            _isServerSideRendering = jsRuntime is IJSInProcessRuntime == false;}

        private async Task<string?> GetTokenAsync()
        {
            // First try to get from localStorage (primary method for JWT)
            if (!_isServerSideRendering)
            {
                try
                {
                    var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "jwt_token");
                    if (!string.IsNullOrEmpty(token))
                    {
                        _logger?.LogInformation("Using token from localStorage");
                        return token;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error getting token from localStorage: {ErrorMessage}", ex.Message);
                }
            }
            
            // Fallback: check if we're authenticated via cookies and can get the token from HttpContext
            if (_httpContextAccessor?.HttpContext?.User?.Identity?.IsAuthenticated == true)
            {
                var tokenClaim = _httpContextAccessor.HttpContext.User.FindFirst("jwt_token");
                if (tokenClaim != null && !string.IsNullOrEmpty(tokenClaim.Value))
                {
                    _logger?.LogInformation("Using token from HttpContext claims");
                    return tokenClaim.Value;
                }
            }
            
            return null;
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
        }        public async Task<T> GetAsync<T>(string endpoint)
        {
            try
            {
                var client = await GetHttpClientAsync();
                var response = await client.GetAsync(endpoint);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    // Handle token refresh or redirect to login
                    await HandleUnauthorized();
                    return default;
                }

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogError("GET request failed for {Endpoint} with status {StatusCode}: {Content}", 
                        endpoint, response.StatusCode, errorContent);
                    
                    // Try to parse error response
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(errorContent, 
                            new JsonSerializerOptions 
                            { 
                                PropertyNameCaseInsensitive = true,
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            });
                        
                        if (errorResponse != null && !string.IsNullOrEmpty(errorResponse.Message))
                        {
                            throw new HttpRequestException($"API request failed: {errorResponse.Message}", null, response.StatusCode);
                        }
                    }
                    catch (JsonException) { /* Continue with default handling */ }
                    
                    throw new HttpRequestException($"API request failed with status code {response.StatusCode}", null, response.StatusCode);
                }
                
                var content = await response.Content.ReadAsStringAsync();
                
                // Handle empty responses gracefully
                if (string.IsNullOrWhiteSpace(content))
                {
                    _logger?.LogWarning("Empty response received from {Endpoint}", endpoint);
                    return default;
                }
                
                // Try to deserialize the response
                try
                {
                    var result = JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    
                    // Handle false success status in response objects that have Success property
                    if (result != null && HasSuccessProperty(result) && !GetSuccessValue(result))
                    {
                        _logger?.LogWarning("API returned success=false for {Endpoint}", endpoint);
                        var message = GetMessageValue(result);
                        if (!string.IsNullOrEmpty(message))
                        {
                            throw new InvalidOperationException($"API operation failed: {message}");
                        }
                        throw new InvalidOperationException("API operation returned success=false without specific message");
                    }
                    
                    return result;
                }
                catch (JsonException ex)
                {
                    _logger?.LogError(ex, "JSON deserialization error for {Endpoint}: {ErrorMessage}", endpoint, ex.Message);
                    _logger?.LogError("Response content: {ResponseContent}", content);
                    throw new InvalidOperationException($"Invalid JSON response from API: {ex.Message}", ex);
                }
            }
            catch (HttpRequestException)
            {
                throw; // Re-throw HTTP exceptions as-is
            }
            catch (InvalidOperationException)
            {
                throw; // Re-throw operation exceptions as-is
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error in GetAsync for {Endpoint}: {ErrorMessage}", endpoint, ex.Message);
                throw new InvalidOperationException($"Unexpected error occurred while calling API: {ex.Message}", ex);
            }
        }        public async Task<T> PostAsync<T>(string endpoint, object data)
        {
            try
            {
                var client = await GetHttpClientAsync();
                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                  _logger?.LogInformation("Making POST request to {Endpoint}", endpoint);
                _logger?.LogInformation("Request data: {Json}", json);
                
                var response = await client.PostAsync(endpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                  _logger?.LogInformation("Response status: {StatusCode}", response.StatusCode);
                _logger?.LogInformation("Response content: {ResponseContent}", responseContent);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {                    // Try to deserialize error message
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
                {                    // Try to deserialize error message
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
                            throw new HttpRequestException($"API request failed: {errorResponse.Message}", null, response.StatusCode);
                        }
                    }
                    catch (JsonException) { /* Continue with default error message */ }
                    
                    throw new HttpRequestException($"API request failed with status code {response.StatusCode}: {responseContent}", null, response.StatusCode);
                }
                
                // Handle empty responses gracefully
                if (string.IsNullOrWhiteSpace(responseContent))
                {
                    _logger?.LogWarning("Empty response received from POST to {Endpoint}", endpoint);
                    return default;
                }
                
                try
                {
                    var result = JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    
                    // Handle false success status in response objects that have Success property
                    if (result != null && HasSuccessProperty(result) && !GetSuccessValue(result))
                    {
                        _logger?.LogWarning("POST API returned success=false for {Endpoint}", endpoint);
                        var message = GetMessageValue(result);
                        if (!string.IsNullOrEmpty(message))
                        {
                            throw new InvalidOperationException($"API operation failed: {message}");
                        }
                        throw new InvalidOperationException("API operation returned success=false without specific message");
                    }
                    
                    return result;
                }
                catch (JsonException ex)
                {                    _logger?.LogError(ex, "JSON deserialization error for POST {Endpoint}: {ErrorMessage}", endpoint, ex.Message);
                    _logger?.LogError("Response content: {ResponseContent}", responseContent);
                    throw new InvalidOperationException($"Invalid JSON response from API: {ex.Message}", ex);
                }
            }
            catch (HttpRequestException)
            {
                throw; // Re-throw HTTP exceptions as-is
            }
            catch (InvalidOperationException)
            {
                throw; // Re-throw operation exceptions as-is
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error in PostAsync for {Endpoint}: {ErrorMessage}", endpoint, ex.Message);
                throw new InvalidOperationException($"Unexpected error occurred while calling API: {ex.Message}", ex);
            }
        }
          public async Task<T> PutAsync<T>(string endpoint, object data)
        {
            try
            {
                var client = await GetHttpClientAsync();
                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                // Log the Authorization header
                _logger?.LogInformation("Authorization Header: {AuthHeader}", client.DefaultRequestHeaders.Authorization);

                var response = await client.PutAsync(endpoint, content);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await HandleUnauthorized();
                    return default;
                }

                // Handle 204 No Content response
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return default; // Return default value for T when no content is expected
                }

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogError("PUT request failed for {Endpoint} with status {StatusCode}: {Content}", 
                        endpoint, response.StatusCode, errorContent);
                    
                    // Try to parse error response
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(errorContent, 
                            new JsonSerializerOptions 
                            { 
                                PropertyNameCaseInsensitive = true,
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            });
                        
                        if (errorResponse != null && !string.IsNullOrEmpty(errorResponse.Message))
                        {
                            throw new HttpRequestException($"API request failed: {errorResponse.Message}", null, response.StatusCode);
                        }
                    }
                    catch (JsonException) { /* Continue with default handling */ }
                    
                    throw new HttpRequestException($"API request failed with status code {response.StatusCode}", null, response.StatusCode);
                }
                
                var responseContent = await response.Content.ReadAsStringAsync();
                
                // Handle empty responses gracefully
                if (string.IsNullOrWhiteSpace(responseContent))
                {
                    _logger?.LogWarning("Empty response received from PUT to {Endpoint}", endpoint);
                    return default;
                }
                
                try
                {
                    var result = JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    
                    // Handle false success status in response objects that have Success property
                    if (result != null && HasSuccessProperty(result) && !GetSuccessValue(result))
                    {
                        _logger?.LogWarning("PUT API returned success=false for {Endpoint}", endpoint);
                        var message = GetMessageValue(result);
                        if (!string.IsNullOrEmpty(message))
                        {
                            throw new InvalidOperationException($"API operation failed: {message}");
                        }
                        throw new InvalidOperationException("API operation returned success=false without specific message");
                    }
                    
                    return result;
                }
                catch (JsonException ex)
                {
                    _logger?.LogError(ex, "JSON deserialization error for PUT {Endpoint}: {ErrorMessage}", endpoint, ex.Message);
                    _logger?.LogError("Response content: {ResponseContent}", responseContent);
                    throw new InvalidOperationException($"Invalid JSON response from API: {ex.Message}", ex);
                }
            }
            catch (HttpRequestException)
            {
                throw; // Re-throw HTTP exceptions as-is
            }
            catch (InvalidOperationException)
            {
                throw; // Re-throw operation exceptions as-is
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error in PutAsync for {Endpoint}: {ErrorMessage}", endpoint, ex.Message);
                throw new InvalidOperationException($"Unexpected error occurred while calling API: {ex.Message}", ex);
            }
        }
          public async Task DeleteAsync(string endpoint)
        {
            try
            {
                var client = await GetHttpClientAsync();
                var response = await client.DeleteAsync(endpoint);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await HandleUnauthorized();
                    return;
                }
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogError("DELETE request failed for {Endpoint} with status {StatusCode}: {Content}", 
                        endpoint, response.StatusCode, errorContent);
                    
                    // Try to parse error response
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(errorContent, 
                            new JsonSerializerOptions 
                            { 
                                PropertyNameCaseInsensitive = true,
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            });
                        
                        if (errorResponse != null && !string.IsNullOrEmpty(errorResponse.Message))
                        {
                            throw new HttpRequestException($"Delete operation failed: {errorResponse.Message}", null, response.StatusCode);
                        }
                    }
                    catch (JsonException) { /* Continue with default handling */ }
                    
                    throw new HttpRequestException($"Delete operation failed with status code {response.StatusCode}", null, response.StatusCode);
                }
            }
            catch (HttpRequestException)
            {
                throw; // Re-throw HTTP exceptions as-is
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error in DeleteAsync for {Endpoint}: {ErrorMessage}", endpoint, ex.Message);
                throw new InvalidOperationException($"Unexpected error occurred while deleting: {ex.Message}", ex);
            }
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

        // New methods for admin file management
        public async Task<string?> UploadFileToStorageAsync(string fileType, IFormFile file, string fileName, string entityName = null)
        {
            var client = await GetHttpClientAsync();
            using var content = new MultipartFormDataContent();
            
            using var fileStream = file.OpenReadStream();
            using var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            
            // Determine the filename with extension
            var fileExtension = Path.GetExtension(file.FileName);
            var fullFileName = fileName + fileExtension;
            
            content.Add(streamContent, "file", fullFileName);
            content.Add(new StringContent(fileType), "fileType");
            content.Add(new StringContent(fileName), "fileName");
            if (!string.IsNullOrEmpty(entityName))
                content.Add(new StringContent(entityName), "entityName");
            
            var response = await client.PostAsync("api/FileStorage/upload", content);
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await HandleUnauthorized();
                return null;
            }
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<FileUploadResultDto>(responseContent, 
                    new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    
                return result?.FilePath;
            }
            
            throw new HttpRequestException($"Upload failed with status code {response.StatusCode}");
        }

        public async Task<string?> DeleteFileFromStorageAsync(string fileType, string fileName)
        {
            var client = await GetHttpClientAsync();
            var data = new { fileType, fileName };
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await client.PostAsync("api/FileStorage/delete", content);
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await HandleUnauthorized();
                return null;
            }
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return responseContent;
            }
            
            throw new HttpRequestException($"Delete failed with status code {response.StatusCode}");
        }

        public async Task<string?> CleanupUnusedFilesAsync()
        {
            var client = await GetHttpClientAsync();
            var response = await client.PostAsync("api/FileStorage/cleanup", null);
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await HandleUnauthorized();
                return null;
            }
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return responseContent;
            }
            
            throw new HttpRequestException($"Cleanup failed with status code {response.StatusCode}");
        }

        public async Task<string?> GenerateThumbnailsAsync()
        {
            var client = await GetHttpClientAsync();
            var response = await client.PostAsync("api/FileStorage/generate-thumbnails", null);
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await HandleUnauthorized();
                return null;
            }
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return responseContent;
            }
            
            throw new HttpRequestException($"Thumbnail generation failed with status code {response.StatusCode}");
        }        private async Task HandleUnauthorized()
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
        }        // Helper methods for checking Success property in response objects
        private bool HasSuccessProperty<T>(T obj)
        {
            if (obj == null) return false;
            var type = obj.GetType();
            var successProperty = type.GetProperty("Success");
            
            // Debug logging to understand what's happening
            _logger?.LogInformation("Checking HasSuccessProperty for type: {TypeName}", type.FullName);
            if (successProperty != null)
            {
                _logger?.LogInformation("Found Success property - Type: {PropertyType}, CanRead: {CanRead}, CanWrite: {CanWrite}", 
                    successProperty.PropertyType, successProperty.CanRead, successProperty.CanWrite);
                
                // Log all properties of the type for debugging
                var allProperties = type.GetProperties();
                _logger?.LogInformation("All properties on type {TypeName}: {Properties}", 
                    type.FullName, string.Join(", ", allProperties.Select(p => $"{p.Name} ({p.PropertyType.Name})")));
            }
            
            return successProperty != null;
        }        private bool GetSuccessValue<T>(T obj)
        {
            if (obj == null) return false;
            var type = obj.GetType();
            var property = type.GetProperty("Success");
            if (property != null && property.PropertyType == typeof(bool))
            {
                var boxedValue = property.GetValue(obj);
                if (boxedValue != null)
                {
                    var value = (bool)boxedValue;
                    _logger?.LogInformation("GetSuccessValue for {TypeName}: Success property value = {Value}", type.FullName, value);
                    return value;
                }
            }
            return true; // Default to true if no Success property
        }

        private string? GetMessageValue<T>(T obj)
        {
            if (obj == null) return null;
            var type = obj.GetType();
            var property = type.GetProperty("Message");
            if (property != null && property.PropertyType == typeof(string))
            {
                return property.GetValue(obj) as string;
            }
            return null;
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
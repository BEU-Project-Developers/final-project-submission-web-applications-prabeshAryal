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
            _isServerSideRendering = jsRuntime is IJSInProcessRuntime == false;}        private async Task<string?> GetTokenAsync()
        {
            _logger?.LogInformation("GetTokenAsync called - checking for available tokens");
            
            // First try to get from localStorage (primary method for JWT)
            if (!_isServerSideRendering)
            {
                try
                {
                    var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "jwt_token");
                    if (!string.IsNullOrEmpty(token))
                    {
                        _logger?.LogInformation("Using token from localStorage (first 10 chars): {TokenPrefix}...", 
                            token.Length > 10 ? token.Substring(0, 10) : token);
                        return token;
                    }
                    else
                    {
                        _logger?.LogInformation("No token found in localStorage");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error getting token from localStorage: {ErrorMessage}", ex.Message);
                }
            }
            else
            {
                _logger?.LogInformation("Server-side rendering detected, skipping localStorage");
            }
            
            // Check if we're authenticated via cookies and can get the token from HttpContext
            if (_httpContextAccessor?.HttpContext?.User?.Identity?.IsAuthenticated == true)
            {
                _logger?.LogInformation("User is authenticated, checking HttpContext for tokens");
                
                // First check HttpContext.Items for updated token
                if (_httpContextAccessor.HttpContext.Items.TryGetValue("jwt_token", out var contextToken) && contextToken is string tokenFromContext)
                {
                    if (!string.IsNullOrEmpty(tokenFromContext))
                    {
                        _logger?.LogInformation("Using token from HttpContext.Items (first 10 chars): {TokenPrefix}...", 
                            tokenFromContext.Length > 10 ? tokenFromContext.Substring(0, 10) : tokenFromContext);
                        return tokenFromContext;
                    }
                }
                else
                {
                    _logger?.LogInformation("No token found in HttpContext.Items");
                }
                
                // Fallback to user claims
                var tokenClaim = _httpContextAccessor.HttpContext.User.FindFirst("jwt_token");
                if (tokenClaim != null && !string.IsNullOrEmpty(tokenClaim.Value))
                {
                    _logger?.LogInformation("Using token from HttpContext claims (first 10 chars): {TokenPrefix}...", 
                        tokenClaim.Value.Length > 10 ? tokenClaim.Value.Substring(0, 10) : tokenClaim.Value);
                    return tokenClaim.Value;
                }
                else
                {
                    _logger?.LogInformation("No token found in HttpContext claims");
                }
            }
            else
            {
                _logger?.LogInformation("User not authenticated via HttpContext");
            }
            
            _logger?.LogWarning("No token found from any source");
            return null;
        }public async Task<HttpClient> GetHttpClientAsync()
        {
            var client = _httpClientFactory.CreateClient("MusicApi");
            client.BaseAddress = new Uri(_baseUrl);
            
            // Add headers for cross-origin requests
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("Origin", "http://localhost:5085");

            var token = await GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                _logger?.LogInformation("Setting Authorization header with token (first 10 chars): {TokenPrefix}...", 
                    token.Length > 10 ? token.Substring(0, 10) : token);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                _logger?.LogWarning("No token available for Authorization header");
            }

            return client;
        }public async Task<T> GetAsync<T>(string endpoint)
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
                    
                    // Check if the response contains a token and update localStorage if needed
                    await HandleTokenInResponse(result);
                    
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
        }          public async Task<T> PutAsync<T>(string endpoint, object data)
        {
            try
            {
                var client = await GetHttpClientAsync();
                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                _logger?.LogInformation("PutAsync: Sending PUT request to {Endpoint}", endpoint);
                _logger?.LogInformation("PutAsync: Request data: {Data}", json);
                _logger?.LogInformation("PutAsync: Authorization Header: {AuthHeader}", client.DefaultRequestHeaders.Authorization);                var response = await client.PutAsync(endpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                _logger?.LogInformation("PutAsync: Response status: {StatusCode}", response.StatusCode);
                _logger?.LogInformation("PutAsync: Response content: {ResponseContent}", responseContent);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger?.LogWarning("PutAsync: Unauthorized response received");
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
                    _logger?.LogError("PUT request failed for {Endpoint} with status {StatusCode}: {Content}", 
                        endpoint, response.StatusCode, responseContent);
                    
                    // Try to parse error response
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
                    catch (JsonException) { /* Continue with default handling */ }
                    
                    throw new HttpRequestException($"API request failed with status code {response.StatusCode}", null, response.StatusCode);
                }
                
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
                    
                    // Check if the response contains a token and update localStorage if needed
                    await HandleTokenInResponse(result);
                    
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
            _logger?.LogInformation("UploadFileAsync: Starting file upload to {Endpoint}", endpoint);
            _logger?.LogInformation("UploadFileAsync: File name: {FileName}, Size: {Size}, ContentType: {ContentType}", 
                file.FileName, file.Length, file.ContentType);
            
            var client = await GetHttpClientAsync();
            using var content = new MultipartFormDataContent();
            
            using var fileStream = file.OpenReadStream();
            using var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            
            content.Add(streamContent, paramName, file.FileName);
            
            _logger?.LogInformation("UploadFileAsync: Sending POST request with multipart content");
            var response = await client.PostAsync(endpoint, content);
            
            _logger?.LogInformation("UploadFileAsync: Response status: {StatusCode}", response.StatusCode);
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger?.LogWarning("UploadFileAsync: Unauthorized response received");
                await HandleUnauthorized();
                return null;
            }
            
            response.EnsureSuccessStatusCode();
              var responseContent = await response.Content.ReadAsStringAsync();
            _logger?.LogInformation("UploadFileAsync: Response content: {ResponseContent}", responseContent);
            
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
        }        private string? GetMessageValue<T>(T obj)
        {
            if (obj == null) return null;
            var type = obj.GetType();
            var property = type.GetProperty("Message");
            if (property != null && property.PropertyType == typeof(string))
            {
                return property.GetValue(obj) as string;
            }
            return null;
        }        private async Task HandleTokenInResponse<T>(T obj)
        {
            _logger?.LogInformation("HandleTokenInResponse called with obj: {ObjType}, _isServerSideRendering: {IsSSR}", 
                obj?.GetType().Name ?? "null", _isServerSideRendering);
                
            if (obj == null) return;
            
            try
            {
                _logger?.LogInformation("HandleTokenInResponse called for type: {TypeName}", obj.GetType().Name);
                var type = obj.GetType();
                var tokenProperty = type.GetProperty("Token");
                
                _logger?.LogInformation("Looking for Token property on type {TypeName}, found: {Found}", 
                    type.Name, tokenProperty != null);
                
                if (tokenProperty != null)
                {
                    _logger?.LogInformation("Token property type: {PropertyType}", tokenProperty.PropertyType.Name);
                    var tokenValue = tokenProperty.GetValue(obj);
                    _logger?.LogInformation("Token value: {TokenValue}", 
                        tokenValue != null ? $"<{tokenValue.ToString()?.Length} chars>" : "null");
                }
                
                if (tokenProperty != null && tokenProperty.PropertyType == typeof(string))
                {
                    var token = tokenProperty.GetValue(obj) as string;
                    if (!string.IsNullOrEmpty(token))
                    {
                        _logger?.LogInformation("Updating JWT token from API response");
                        
                        // Always try to update localStorage first - this is the primary storage for JWTs
                        try
                        {
                            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "jwt_token", token);
                            _logger?.LogInformation("Successfully updated token in localStorage");
                        }
                        catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop") || ex.Message.Contains("prerendering"))
                        {
                            _logger?.LogWarning("Cannot update localStorage during server-side rendering: {ErrorMessage}", ex.Message);
                            
                            // For server-side scenarios, we need to handle this differently
                            // Since HttpContext.Items only persists for the current request, 
                            // we need to ensure the token gets to the client-side for subsequent requests
                            
                            if (_httpContextAccessor?.HttpContext != null)
                            {
                                try
                                {
                                    _logger?.LogInformation("Storing token in HttpContext.Items as fallback for current request");
                                    _httpContextAccessor.HttpContext.Items["jwt_token"] = token;
                                    
                                    // Also try to update the current user's claims with the new token
                                    if (_httpContextAccessor.HttpContext.User.Identity is System.Security.Claims.ClaimsIdentity identity)
                                    {
                                        // Remove old token claim if it exists
                                        var oldTokenClaim = identity.FindFirst("jwt_token");
                                        if (oldTokenClaim != null)
                                        {
                                            identity.RemoveClaim(oldTokenClaim);
                                        }
                                        
                                        // Add new token claim
                                        identity.AddClaim(new System.Security.Claims.Claim("jwt_token", token));
                                        _logger?.LogInformation("Updated JWT token in user claims");
                                    }
                                    
                                    // IMPORTANT: For server-side scenarios, we need to pass the token to the client
                                    // Add a custom header that the client can read and store in localStorage
                                    _httpContextAccessor.HttpContext.Response.Headers.Add("X-Updated-Token", token);
                                    _logger?.LogInformation("Added X-Updated-Token header for client-side update");
                                }
                                catch (Exception httpEx)
                                {
                                    _logger?.LogError(httpEx, "Failed to store token in HttpContext: {ErrorMessage}", httpEx.Message);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "Failed to update token in localStorage: {ErrorMessage}", ex.Message);
                        }
                    }
                    else
                    {
                        _logger?.LogInformation("Token property found but value is null or empty");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating token from response: {ErrorMessage}", ex.Message);
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
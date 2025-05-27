using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;

namespace MusicAppBackend.Middleware
{
    public class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

        public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred while processing the request");
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            
            var response = new
            {
                error = "An error occurred while processing your request",
                details = exception.Message,
                timestamp = DateTime.UtcNow
            };

            switch (exception)
            {
                case DbUpdateConcurrencyException:
                    context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                    response = new
                    {
                        error = "A concurrency conflict occurred",
                        details = "The resource was modified by another user. Please refresh and try again.",
                        timestamp = DateTime.UtcNow
                    };
                    break;

                case DbUpdateException when exception.Message.Contains("FOREIGN KEY constraint"):
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response = new
                    {
                        error = "Foreign key constraint violation",
                        details = "Cannot perform this operation due to existing relationships",
                        timestamp = DateTime.UtcNow
                    };
                    break;

                case DbUpdateException when exception.Message.Contains("UNIQUE constraint"):
                    context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                    response = new
                    {
                        error = "Duplicate entry",
                        details = "A record with this information already exists",
                        timestamp = DateTime.UtcNow
                    };
                    break;

                case InvalidOperationException when exception.Message.Contains("Invalid object name") ||
                                                  exception.Message.Contains("doesn't exist"):
                    context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                    response = new
                    {
                        error = "Database not properly initialized",
                        details = "Please ensure the database is set up correctly and migrations are applied",
                        timestamp = DateTime.UtcNow
                    };
                    break;

                case TimeoutException:
                    context.Response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                    response = new
                    {
                        error = "Request timeout",
                        details = "The operation took too long to complete",
                        timestamp = DateTime.UtcNow
                    };
                    break;

                case UnauthorizedAccessException:
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    response = new
                    {
                        error = "Unauthorized",
                        details = "You don't have permission to perform this action",
                        timestamp = DateTime.UtcNow
                    };
                    break;                case ArgumentNullException:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response = new
                    {
                        error = "Invalid input",
                        details = "A required parameter was null or empty",
                        timestamp = DateTime.UtcNow
                    };
                    break;

                case ArgumentException:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response = new
                    {
                        error = "Invalid input",
                        details = exception.Message,
                        timestamp = DateTime.UtcNow
                    };
                    break;

                case FileNotFoundException:
                case DirectoryNotFoundException:
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    response = new
                    {
                        error = "File not found",
                        details = "The requested file could not be found",
                        timestamp = DateTime.UtcNow
                    };
                    break;

                case IOException:
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response = new
                    {
                        error = "File operation failed",
                        details = "An error occurred while processing the file",
                        timestamp = DateTime.UtcNow
                    };
                    break;

                default:
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response = new
                    {
                        error = "Internal server error",
                        details = "An unexpected error occurred",
                        timestamp = DateTime.UtcNow
                    };
                    break;
            }

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }
    }
}

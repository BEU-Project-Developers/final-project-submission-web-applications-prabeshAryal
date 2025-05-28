using Microsoft.AspNetCore.Mvc;
using MusicApp.Services;
using System.Net;

namespace MusicApp.Controllers
{
    /// <summary>
    /// Base controller with standardized error handling for all application controllers
    /// </summary>
    public abstract class BaseAppController : Controller
    {
        protected readonly ApiService _apiService;
        protected readonly ILogger _logger;

        protected BaseAppController(ApiService apiService, ILogger logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        /// <summary>
        /// Handles API exceptions with consistent error messaging and logging
        /// </summary>
        /// <typeparam name="T">Return type for successful operations</typeparam>
        /// <param name="operation">The operation to execute</param>
        /// <param name="fallbackValue">Value to return on error</param>
        /// <param name="userErrorMessage">User-friendly error message</param>
        /// <param name="logContext">Additional context for logging</param>
        /// <returns>Result of operation or fallback value</returns>
        protected async Task<T> SafeApiCall<T>(
            Func<Task<T>> operation, 
            T fallbackValue, 
            string userErrorMessage, 
            string logContext = null)
        {
            try
            {
                var result = await operation();
                
                // Handle null results gracefully
                if (result == null)
                {
                    _logger?.LogWarning("API call returned null result for {Context}", logContext ?? "unknown operation");
                    return fallbackValue;
                }
                
                return result;
            }
            catch (HttpRequestException httpEx)
            {
                return HandleHttpException(httpEx, fallbackValue, userErrorMessage, logContext);
            }
            catch (InvalidOperationException invOpEx)
            {
                return HandleInvalidOperationException(invOpEx, fallbackValue, userErrorMessage, logContext);
            }
            catch (Exception ex)
            {
                return HandleGenericException(ex, fallbackValue, userErrorMessage, logContext);
            }
        }

        /// <summary>
        /// Handles API operations that return ActionResult with consistent error handling
        /// </summary>
        /// <param name="operation">The operation to execute</param>
        /// <param name="fallbackAction">Action to return on error</param>
        /// <param name="userErrorMessage">User-friendly error message</param>
        /// <param name="logContext">Additional context for logging</param>
        /// <returns>ActionResult</returns>
        protected async Task<IActionResult> SafeApiAction(
            Func<Task<IActionResult>> operation,
            Func<IActionResult> fallbackAction,
            string userErrorMessage,
            string logContext = null)
        {
            try
            {
                return await operation();
            }
            catch (HttpRequestException httpEx)
            {
                HandleHttpException(httpEx, default(object), userErrorMessage, logContext);
                return fallbackAction();
            }
            catch (InvalidOperationException invOpEx)
            {
                HandleInvalidOperationException(invOpEx, default(object), userErrorMessage, logContext);
                return fallbackAction();
            }
            catch (Exception ex)
            {
                HandleGenericException(ex, default(object), userErrorMessage, logContext);
                return fallbackAction();
            }
        }

        private T HandleHttpException<T>(HttpRequestException httpEx, T fallbackValue, string userErrorMessage, string logContext)
        {
            var statusCode = httpEx.StatusCode;
            
            if (statusCode == HttpStatusCode.NotFound)
            {
                _logger?.LogWarning("Resource not found for {Context}: {Message}", logContext ?? "unknown", httpEx.Message);
                ViewBag.ErrorMessage = "The requested resource was not found.";
            }
            else if (statusCode == HttpStatusCode.Unauthorized)
            {
                _logger?.LogWarning("Unauthorized access for {Context}: {Message}", logContext ?? "unknown", httpEx.Message);
                ViewBag.ErrorMessage = "You are not authorized to perform this action. Please log in again.";
            }
            else if (statusCode == HttpStatusCode.Forbidden)
            {
                _logger?.LogWarning("Forbidden access for {Context}: {Message}", logContext ?? "unknown", httpEx.Message);
                ViewBag.ErrorMessage = "You do not have permission to perform this action.";
            }
            else if (statusCode >= HttpStatusCode.InternalServerError)
            {
                _logger?.LogError(httpEx, "Server error for {Context}: {Message}", logContext ?? "unknown", httpEx.Message);
                ViewBag.ErrorMessage = "A server error occurred. Please try again later.";
            }
            else
            {
                _logger?.LogError(httpEx, "HTTP error for {Context}: {Message}", logContext ?? "unknown", httpEx.Message);
                ViewBag.ErrorMessage = userErrorMessage ?? "An error occurred while processing your request.";
            }
            
            return fallbackValue;
        }

        private T HandleInvalidOperationException<T>(InvalidOperationException invOpEx, T fallbackValue, string userErrorMessage, string logContext)
        {
            _logger?.LogError(invOpEx, "Invalid operation for {Context}: {Message}", logContext ?? "unknown", invOpEx.Message);
            
            // Check if this is an API operation failure with a specific message
            if (invOpEx.Message.StartsWith("API operation failed:"))
            {
                var apiMessage = invOpEx.Message.Replace("API operation failed:", "").Trim();
                ViewBag.ErrorMessage = !string.IsNullOrEmpty(apiMessage) ? apiMessage : userErrorMessage;
            }
            else
            {
                ViewBag.ErrorMessage = userErrorMessage ?? "The operation could not be completed.";
            }
            
            return fallbackValue;
        }

        private T HandleGenericException<T>(Exception ex, T fallbackValue, string userErrorMessage, string logContext)
        {
            _logger?.LogError(ex, "Unexpected error for {Context}: {Message}", logContext ?? "unknown", ex.Message);
            ViewBag.ErrorMessage = userErrorMessage ?? "An unexpected error occurred. Please try again later.";
            return fallbackValue;
        }

        /// <summary>
        /// Sets a success message in TempData
        /// </summary>
        /// <param name="message">Success message to display</param>
        protected void SetSuccessMessage(string message)
        {
            TempData["SuccessMessage"] = message;
        }

        /// <summary>
        /// Sets an error message in TempData
        /// </summary>
        /// <param name="message">Error message to display</param>
        protected void SetErrorMessage(string message)
        {
            TempData["ErrorMessage"] = message;
        }

        /// <summary>
        /// Gets user-friendly error message for common API operations
        /// </summary>
        /// <param name="operation">The operation type</param>
        /// <param name="entityName">Name of the entity being operated on</param>
        /// <returns>User-friendly error message</returns>
        protected string GetStandardErrorMessage(string operation, string entityName)
        {
            return operation.ToLower() switch
            {
                "get" or "load" or "retrieve" => $"Unable to load {entityName}. Please try again later.",
                "create" or "add" => $"Unable to create {entityName}. Please check your input and try again.",
                "update" or "edit" => $"Unable to update {entityName}. Please check your input and try again.",
                "delete" or "remove" => $"Unable to delete {entityName}. Please try again later.",
                _ => $"Unable to {operation} {entityName}. Please try again later."
            };
        }
    }
}

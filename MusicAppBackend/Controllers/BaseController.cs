using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MusicAppBackend.Data;

namespace MusicAppBackend.Controllers
{
    [ApiController]
    public abstract class BaseController : ControllerBase
    {
        protected readonly MusicDbContext _context;
        protected readonly ILogger _logger;

        protected BaseController(MusicDbContext context, ILogger logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Handles database operations safely with proper error handling
        /// </summary>
        protected async Task<ActionResult<T>> HandleDatabaseOperation<T>(
            Func<Task<T>> operation, 
            string operationName = "Database operation")
        {
            try
            {
                // Check if database is accessible
                if (!await _context.Database.CanConnectAsync())
                {
                    _logger.LogError("Database connection failed during {OperationName}", operationName);
                    return StatusCode(503, new { error = "Database service is currently unavailable" });
                }

                var result = await operation();
                
                if (result == null)
                {
                    _logger.LogWarning("No data found for {OperationName}", operationName);
                    return NotFound(new { message = "No data found" });
                }

                return Ok(result);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update error during {OperationName}", operationName);
                return StatusCode(500, new { error = "Failed to update database", details = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation during {OperationName}", operationName);
                return BadRequest(new { error = "Invalid operation", details = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid argument during {OperationName}", operationName);
                return BadRequest(new { error = "Invalid input", details = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during {OperationName}", operationName);
                return StatusCode(500, new { error = "An unexpected error occurred", details = ex.Message });
            }
        }

        /// <summary>
        /// Handles database operations for collections with empty table handling
        /// </summary>
        protected async Task<ActionResult<IEnumerable<T>>> HandleCollectionOperation<T>(
            Func<Task<IEnumerable<T>>> operation,
            string operationName = "Collection operation")
        {
            try
            {
                // Check if database is accessible
                if (!await _context.Database.CanConnectAsync())
                {
                    _logger.LogError("Database connection failed during {OperationName}", operationName);
                    return StatusCode(503, new { error = "Database service is currently unavailable" });
                }

                var result = await operation();
                
                if (result == null || !result.Any())
                {
                    _logger.LogInformation("No items found for {OperationName}", operationName);
                    return Ok(new List<T>()); // Return empty list instead of 404 for collections
                }

                return Ok(result);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update error during {OperationName}", operationName);
                return StatusCode(500, new { error = "Failed to update database", details = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation during {OperationName}", operationName);
                return BadRequest(new { error = "Invalid operation", details = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid argument during {OperationName}", operationName);
                return BadRequest(new { error = "Invalid input", details = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during {OperationName}", operationName);
                return StatusCode(500, new { error = "An unexpected error occurred", details = ex.Message });
            }
        }

        /// <summary>
        /// Validates pagination parameters
        /// </summary>
        protected (int page, int pageSize) ValidatePagination(int page, int pageSize, int maxPageSize = 100)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > maxPageSize) pageSize = 20;
            return (page, pageSize);
        }        /// <summary>
        /// Handles database operations for paginated responses
        /// </summary>
        protected async Task<ActionResult> HandlePaginatedOperation(
            Func<Task<object>> operation,
            string operationName = "Paginated operation")
        {
            try
            {
                // Check if database is accessible
                if (!await _context.Database.CanConnectAsync())
                {
                    _logger.LogError("Database connection failed during {OperationName}", operationName);
                    return StatusCode(503, new { error = "Database service is currently unavailable" });
                }

                var result = await operation();
                return Ok(result);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update error during {OperationName}", operationName);
                return StatusCode(500, new { error = "Failed to update database", details = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation during {OperationName}", operationName);
                return BadRequest(new { error = "Invalid operation", details = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid argument during {OperationName}", operationName);
                return BadRequest(new { error = "Invalid input", details = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during {OperationName}", operationName);
                return StatusCode(500, new { error = "An unexpected error occurred", details = ex.Message });
            }
        }

        /// <summary>
        /// Handles file upload operations safely
        /// </summary>
        protected async Task<ActionResult<string>> HandleFileUpload(
            IFormFile file,
            string uploadPath,
            Func<IFormFile, string, Task<string>> uploadOperation,
            string operationName = "File upload")
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { error = "No file provided or file is empty" });
                }

                // Validate file size (max 10MB)
                if (file.Length > 10 * 1024 * 1024)
                {
                    return BadRequest(new { error = "File size exceeds the maximum limit of 10MB" });
                }

                var result = await uploadOperation(file, uploadPath);
                return Ok(new { filePath = result });
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid file argument during {OperationName}", operationName);
                return BadRequest(new { error = "Invalid file", details = ex.Message });
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "File I/O error during {OperationName}", operationName);
                return StatusCode(500, new { error = "File operation failed", details = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during {OperationName}", operationName);
                return StatusCode(500, new { error = "An unexpected error occurred", details = ex.Message });
            }
        }

        /// <summary>
        /// Creates a paginated response with metadata
        /// </summary>
        protected object CreatePaginatedResponse<T>(IEnumerable<T> items, int page, int pageSize, int totalCount)
        {
            return new
            {
                data = items,
                pagination = new
                {
                    currentPage = page,
                    pageSize = pageSize,
                    totalCount = totalCount,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    hasNext = page * pageSize < totalCount,
                    hasPrevious = page > 1
                }
            };
        }
    }
}

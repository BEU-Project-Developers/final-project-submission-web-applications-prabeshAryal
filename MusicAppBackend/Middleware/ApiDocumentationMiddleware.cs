// Middleware/ApiDocumentationMiddleware.cs
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.Text;
using System.Text.Json;

namespace MusicAppBackend.Middleware
{
    public class ApiDocumentationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;

        public ApiDocumentationMiddleware(RequestDelegate next, IActionDescriptorCollectionProvider actionDescriptorCollectionProvider)
        {
            _next = next;
            _actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.Value?.ToLower() == "/api" &&
                context.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
                var routes = _actionDescriptorCollectionProvider.ActionDescriptors.Items
                    .Where(x => x.AttributeRouteInfo != null)
                    .Select(x => new
                    {
                        Controller = x.RouteValues.ContainsKey("controller") ? x.RouteValues["controller"] : null,
                        Action = x.RouteValues.ContainsKey("action") ? x.RouteValues["action"] : null,
                        Path = x.AttributeRouteInfo?.Template,
                        HttpMethods = x.ActionConstraints?.OfType<HttpMethodActionConstraint>()
                                      .FirstOrDefault()?.HttpMethods ?? new string[] { "GET" }
                    })
                    .OrderBy(r => r.Path)
                    .ToList();

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(routes,
                    new JsonSerializerOptions { WriteIndented = true }));
                return;
            }

            await _next(context);
        }
    }

    // Extension method to make it easier to add the middleware
    public static class ApiDocumentationMiddlewareExtensions
    {
        public static IApplicationBuilder UseApiDocumentation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ApiDocumentationMiddleware>();
        }
    }
}
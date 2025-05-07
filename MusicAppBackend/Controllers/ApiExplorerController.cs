// Controllers/ApiExplorerController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace MusicAppBackend.Controllers
{
    [Route("api")]
    [ApiController]
    public class ApiExplorerController : ControllerBase
    {
        private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;

        public ApiExplorerController(IActionDescriptorCollectionProvider actionDescriptorCollectionProvider)
        {
            _actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
        }

        [HttpGet]
        public IActionResult GetAllRoutes()
        {
            var routes = _actionDescriptorCollectionProvider.ActionDescriptors.Items.Select(x => new
            {
                Action = x.DisplayName,
                Controller = x.RouteValues.ContainsKey("controller") ? x.RouteValues["controller"] : null,
                Path = x.AttributeRouteInfo?.Template,
                HttpMethods = x.ActionConstraints?.OfType<HttpMethodActionConstraint>().FirstOrDefault()?.HttpMethods
            }).Where(r => r.Path != null);

            return Ok(routes);
        }
    }
}
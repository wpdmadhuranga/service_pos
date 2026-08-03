using backend.Application.DTOs.Services;
using backend.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.API.Controllers
{
    [ApiController]
    [Route("api/admin/services")]
    public class ServicesController : ControllerBase
    {
        private readonly IServiceAdminService _serviceAdminService;

        public ServicesController(IServiceAdminService serviceAdminService)
        {
            _serviceAdminService = serviceAdminService;
        }

        [HttpPost]
        public async Task<ActionResult<ServiceDto>> Create([FromBody] ServiceCreateRequest request, CancellationToken cancellationToken)
        {
            return await HandleAsync(() => _serviceAdminService.CreateAsync(request, cancellationToken), created: true);
        }

        [HttpPatch("{id:guid}")]
        public async Task<ActionResult<ServiceDto>> Update(Guid id, [FromBody] ServiceUpdateRequest request, CancellationToken cancellationToken)
        {
            return await HandleAsync(() => _serviceAdminService.UpdateAsync(id, request, cancellationToken));
        }

        private async Task<ActionResult<ServiceDto>> HandleAsync(Func<Task<ServiceDto>> action, bool created = false)
        {
            try
            {
                var result = await action();
                return created ? Created($"/api/admin/services/{result.Id}", result) : Ok(result);
            }
            catch (InvalidOperationException exception)
            {
                if (exception.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    return NotFound(new { message = exception.Message });
                }

                return BadRequest(new { message = exception.Message });
            }
        }
    }
}
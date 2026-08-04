using backend.Application.DTOs.Inventory;
using backend.Application.Services;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace backend.API.Controllers
{
    [ApiController]
    [Route("api/inventory/products")]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;

        public InventoryController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetProducts([FromQuery] Guid? serviceId, CancellationToken cancellationToken)
        {
            return Ok(await _inventoryService.GetProductsAsync(serviceId, cancellationToken));
        }

        [HttpPost]
        public async Task<ActionResult<ProductDto>> Create([FromBody] ProductCreateRequest request, CancellationToken cancellationToken)
        {
            return await HandleAsync(() => _inventoryService.CreateProductAsync(request, cancellationToken), created: true);
        }

        [HttpPatch("{id:guid}")]
        public async Task<ActionResult<ProductDto>> Update(Guid id, [FromBody] ProductUpdateRequest request, CancellationToken cancellationToken)
        {
            return await HandleAsync(() => _inventoryService.UpdateProductAsync(id, request, cancellationToken));
        }

        [HttpPost("{id:guid}/stock-in")]
        public async Task<ActionResult<ProductDto>> StockIn(Guid id, [FromBody] StockInRequest request, CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            return await HandleAsync(() => _inventoryService.StockInAsync(id, request, userId, cancellationToken));
        }

        [HttpGet("low-stock")]
        public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetLowStock([FromQuery] int threshold = 5, CancellationToken cancellationToken = default)
        {
            return Ok(await _inventoryService.GetLowStockAsync(threshold, cancellationToken));
        }

        private async Task<ActionResult<ProductDto>> HandleAsync(Func<Task<ProductDto>> action, bool created = false)
        {
            try
            {
                var result = await action();
                return created ? Created($"/api/inventory/products/{result.Id}", result) : Ok(result);
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

        private Guid GetCurrentUserId()
        {
            var rawUserId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(rawUserId, out var userId))
            {
                throw new InvalidOperationException("Authenticated user is required.");
            }

            return userId;
        }
    }
}
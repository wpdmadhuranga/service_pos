using backend.Application.DTOs.Inventory;
using backend.Application.Services;
using Microsoft.AspNetCore.Mvc;
using backend.Application.Services;
using backend.Application.Common.Interfaces;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace backend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;

        public InventoryController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllItems(CancellationToken cancellationToken)
        {
            var items = await _inventoryService.GetAllItemsAsync(cancellationToken);
            return Ok(items);
        }

        [HttpPost]
        public async Task<IActionResult> CreateItem([FromBody] CreateInventoryItemDto dto, CancellationToken cancellationToken)
        {
            var item = await _inventoryService.CreateItemAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetAllItems), new { id = item.Id }, item);
        }

        [HttpPost("{id}/stock-in")]
        public async Task<IActionResult> StockIn(Guid id, [FromBody] StockAdjustmentDto dto, CancellationToken cancellationToken)
        {
            dto.UserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _inventoryService.AdjustStockAsync(id, dto, true, cancellationToken);
            return Ok(new { message = "Stock added successfully." });
        }

        [HttpPost("{id}/stock-out")]
        public async Task<IActionResult> StockOut(Guid id, [FromBody] StockAdjustmentDto dto, CancellationToken cancellationToken)
        {
            dto.UserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            await _inventoryService.AdjustStockAsync(id, dto, false, cancellationToken);

            return Ok(new { message = "Stock removed successfully." });
        }
    }
}
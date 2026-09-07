using backend.Application.DTOs.Inventory;

namespace backend.Application.Services
{
    public interface IInventoryService
    {

        Task<ProductDto> CreateProductAsync(ProductCreateRequest request, CancellationToken cancellationToken = default);

        Task<ProductDto> UpdateProductAsync(Guid id, ProductUpdateRequest request, CancellationToken cancellationToken = default);

        Task<ProductDto> StockInAsync(Guid id, StockInRequest request, Guid userId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ProductDto>> GetLowStockAsync(int threshold, CancellationToken cancellationToken = default);

        Task<IEnumerable<InventoryItemResponseDto>> GetAllItemsAsync(CancellationToken cancellationToken);

        Task<InventoryItemResponseDto> GetItemByIdAsync(Guid itemId, CancellationToken cancellationToken);

        Task<IEnumerable<InventoryItemResponseDto>> GetItemsByNameAsync(string name, CancellationToken cancellationToken);

        Task<InventoryItemResponseDto> CreateItemAsync(CreateInventoryItemDto dto, CancellationToken cancellationToken);

        Task AdjustStockAsync(Guid itemId, StockAdjustmentDto dto, bool isStockIn, CancellationToken cancellationToken);
    }
}
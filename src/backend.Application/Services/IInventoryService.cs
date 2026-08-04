using backend.Application.DTOs.Inventory;

namespace backend.Application.Services
{
    public interface IInventoryService
    {
        Task<IReadOnlyList<ProductDto>> GetProductsAsync(Guid? serviceId, CancellationToken cancellationToken = default);
        Task<ProductDto> CreateProductAsync(ProductCreateRequest request, CancellationToken cancellationToken = default);
        Task<ProductDto> UpdateProductAsync(Guid id, ProductUpdateRequest request, CancellationToken cancellationToken = default);
        Task<ProductDto> StockInAsync(Guid id, StockInRequest request, Guid userId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ProductDto>> GetLowStockAsync(int threshold, CancellationToken cancellationToken = default);
    }
}
using backend.Application.Common.Interfaces;
using backend.Application.DTOs.Inventory;
using backend.Application.Services;
using backend.Domain.Entities;
using backend.Domain.Enums;
// using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Inventory.Service
{
    public sealed class InventoryService : IInventoryService
    {
        private readonly IApplicationDbContext _db;

        public InventoryService(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(Guid? serviceId, CancellationToken cancellationToken = default)
        {
            var query = _db.Products
                .AsNoTracking()
                .Include(product => product.Service)
                .AsQueryable();

            if (serviceId.HasValue)
            {
                query = query.Where(product => product.ServiceId == serviceId.Value);
            }

            return await query
                .OrderBy(product => product.Brand)
                .ThenBy(product => product.Name)
                .Select(product => ToDto(product))
                .ToListAsync(cancellationToken);
        }

        public async Task<ProductDto> CreateProductAsync(ProductCreateRequest request, CancellationToken cancellationToken = default)
        {
            var product = new Product
            {
                Id = Guid.NewGuid(),
                ServiceId = request.ServiceId,
                Brand = request.Brand.Trim(),
                Name = request.Name.Trim(),
                PartNumber = string.IsNullOrWhiteSpace(request.PartNumber) ? null : request.PartNumber.Trim(),
                CompatibleVehicleType = string.IsNullOrWhiteSpace(request.CompatibleVehicleType) ? null : request.CompatibleVehicleType.Trim(),
                CostPrice = request.CostPrice,
                SellingPrice = request.SellingPrice,
                StockQuantity = request.StockQuantity,
                Unit = string.IsNullOrWhiteSpace(request.Unit) ? null : request.Unit.Trim(),
                IsActive = request.IsActive
            };

            await ValidateProductAsync(product, cancellationToken);
            _db.Add(product);
            await _db.SaveChangesAsync(cancellationToken);

            return await LoadDtoAsync(product.Id, cancellationToken);
        }

        public async Task<ProductDto> UpdateProductAsync(Guid id, ProductUpdateRequest request, CancellationToken cancellationToken = default)
        {
            var product = await _db.Products.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
                ?? throw new InvalidOperationException("Product was not found.");

            if (request.ServiceId.HasValue)
            {
                product.ServiceId = request.ServiceId.Value;
            }

            if (request.Brand is not null)
            {
                product.Brand = request.Brand.Trim();
            }

            if (request.Name is not null)
            {
                product.Name = request.Name.Trim();
            }

            if (request.PartNumber is not null)
            {
                product.PartNumber = string.IsNullOrWhiteSpace(request.PartNumber) ? null : request.PartNumber.Trim();
            }

            if (request.CompatibleVehicleType is not null)
            {
                product.CompatibleVehicleType = string.IsNullOrWhiteSpace(request.CompatibleVehicleType) ? null : request.CompatibleVehicleType.Trim();
            }

            if (request.CostPrice.HasValue)
            {
                product.CostPrice = request.CostPrice.Value;
            }

            if (request.SellingPrice.HasValue)
            {
                product.SellingPrice = request.SellingPrice.Value;
            }

            if (request.StockQuantity.HasValue)
            {
                product.StockQuantity = request.StockQuantity.Value;
            }

            if (request.Unit is not null)
            {
                product.Unit = string.IsNullOrWhiteSpace(request.Unit) ? null : request.Unit.Trim();
            }

            if (request.IsActive.HasValue)
            {
                product.IsActive = request.IsActive.Value;
            }

            await ValidateProductAsync(product, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return await LoadDtoAsync(product.Id, cancellationToken);
        }

        public async Task<ProductDto> StockInAsync(Guid id, StockInRequest request, Guid userId, CancellationToken cancellationToken = default)
        {
            var product = await _db.Products.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
                ?? throw new InvalidOperationException("Product was not found.");

            product.StockQuantity += request.Quantity;

            _db.Add(new InventoryTransaction
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Type = InventoryTransactionType.StockIn,
                Quantity = request.Quantity,
                Notes = request.Notes,
                UserId = userId
            });

            await _db.SaveChangesAsync(cancellationToken);
            return await LoadDtoAsync(product.Id, cancellationToken);
        }

        public async Task<IReadOnlyList<ProductDto>> GetLowStockAsync(int threshold, CancellationToken cancellationToken = default)
        {
            return await _db.Products
                .AsNoTracking()
                .Include(product => product.Service)
                .Where(product => product.StockQuantity < threshold)
                .OrderBy(product => product.StockQuantity)
                .ThenBy(product => product.Brand)
                .Select(product => ToDto(product))
                .ToListAsync(cancellationToken);
        }

        private async Task ValidateProductAsync(Product product, CancellationToken cancellationToken)
        {
            if (product.ServiceId.HasValue)
            {
                var serviceExists = await _db.Services.AnyAsync(service => service.Id == product.ServiceId.Value, cancellationToken);
                if (!serviceExists)
                {
                    throw new InvalidOperationException("Service was not found.");
                }
            }

            if (product.CostPrice < 0m || product.SellingPrice < 0m)
            {
                throw new InvalidOperationException("Prices must be non-negative.");
            }
        }

        private async Task<ProductDto> LoadDtoAsync(Guid productId, CancellationToken cancellationToken)
        {
            var product = await _db.Products
                .AsNoTracking()
                .Include(item => item.Service)
                .FirstAsync(item => item.Id == productId, cancellationToken);

            return ToDto(product);
        }

        private static ProductDto ToDto(Product product)
        {
            return new ProductDto(
                product.Id,
                product.ServiceId,
                product.Brand,
                product.Name,
                product.PartNumber,
                product.CompatibleVehicleType,
                product.CostPrice,
                product.SellingPrice,
                product.StockQuantity,
                product.Unit,
                product.IsActive,
                product.CreatedAt,
                product.UpdatedAt);
        }

    }
}
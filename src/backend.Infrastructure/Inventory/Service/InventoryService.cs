using backend.Application.Common.Interfaces;
using backend.Application.DTOs.Inventory;
using backend.Application.Services;
using backend.Domain.Entities;
using backend.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using InventoryItemEntity = backend.Domain.Entities.Inventory.InventoryItem;
using InventoryItemTransaction = backend.Domain.Entities.Inventory.InventoryTransaction;

namespace backend.Infrastructure.Inventory.Service
{
    public sealed class InventoryService : IInventoryService
    {
        private readonly IApplicationDbContext _db;

        public InventoryService(IApplicationDbContext db)
        {
            _db = db;
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

            return await LoadProductDtoAsync(product.Id, cancellationToken);
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
            return await LoadProductDtoAsync(product.Id, cancellationToken);
        }

        public async Task<ProductDto> StockInAsync(Guid id, StockInRequest request, Guid userId, CancellationToken cancellationToken = default)
        {
            var product = await _db.Products.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
                ?? throw new InvalidOperationException("Product was not found.");

            product.StockQuantity += request.Quantity;
            product.UpdatedAt = DateTime.UtcNow;

            _db.Add(new InventoryTransaction
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Type = InventoryTransactionType.StockIn,
                Quantity = request.Quantity,
                Notes = request.Notes,
                UserId = userId
            });

            if (product.InventoryItemId is Guid linkedItemId)
            {
                var linkedItem = await _db.InventoryItems
                    .FirstOrDefaultAsync(i => i.Id == linkedItemId, cancellationToken);

                if (linkedItem is not null)
                {
                    linkedItem.QuantityOnHand += request.Quantity;
                    linkedItem.UpdatedAt = DateTime.UtcNow;

                    _db.Add(new InventoryItemTransaction
                    {
                        Id = Guid.NewGuid(),
                        InventoryItemId = linkedItem.Id,
                        Type = InventoryTransactionType.StockIn,
                        Quantity = request.Quantity,
                        Note = $"Synced from Product stock-in ({request.Notes})",
                        CreatedBy = userId,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            return await LoadProductDtoAsync(product.Id, cancellationToken);
        }

        public async Task<IReadOnlyList<ProductDto>> GetLowStockAsync(int threshold, CancellationToken cancellationToken = default)
        {
            return await _db.Products
                .AsNoTracking()
                .Include(product => product.Service)
                .Where(product => product.StockQuantity < threshold)
                .OrderBy(product => product.StockQuantity)
                .ThenBy(product => product.Brand)
                .Select(product => ToProductDto(product))
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

        private async Task<ProductDto> LoadProductDtoAsync(Guid productId, CancellationToken cancellationToken)
        {
            var product = await _db.Products
                .AsNoTracking()
                .Include(item => item.Service)
                .FirstAsync(item => item.Id == productId, cancellationToken);

            return ToProductDto(product);
        }

        private static ProductDto ToProductDto(Product product)
        {
            return new ProductDto(
                product.Id,
                product.ServiceId,
                product.InventoryItemId,
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

        public async Task<IEnumerable<InventoryItemResponseDto>> GetAllItemsAsync(CancellationToken cancellationToken)
        {
            var items = await _db.InventoryItems
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var productsByItemId = await GetLinkedProductsByItemIdsAsync(
                items.Select(i => i.Id), cancellationToken);

            return items.Select(item =>
            {
                productsByItemId.TryGetValue(item.Id, out var product);
                return MapToDto(item, product);
            });
        }

        public async Task<IEnumerable<InventoryItemResponseDto>> GetItemsByNameAsync(string name, CancellationToken cancellationToken)
        {
            var items = await _db.InventoryItems
                .AsNoTracking()
                .Where(i => i.Name.Contains(name))
                .ToListAsync(cancellationToken);

            var productsByItemId = await GetLinkedProductsByItemIdsAsync(
                items.Select(i => i.Id), cancellationToken);

            return items.Select(item =>
            {
                productsByItemId.TryGetValue(item.Id, out var product);
                return MapToDto(item, product);
            });
        }

        public async Task<InventoryItemResponseDto> GetItemByIdAsync(Guid itemId, CancellationToken cancellationToken)
        {
            var item = await _db.InventoryItems
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken)
                ?? throw new InvalidOperationException("Inventory item was not found.");

            var product = await _db.Products
                .AsNoTracking()
                .Include(p => p.Service)
                    .ThenInclude(s => s!.Category)
                .FirstOrDefaultAsync(p => p.InventoryItemId == itemId, cancellationToken);

            return MapToDto(item, product);
        }

        public async Task<InventoryItemResponseDto> CreateItemAsync(CreateInventoryItemDto dto, CancellationToken cancellationToken)
        {
            var item = new InventoryItemEntity
            {
                Id = Guid.NewGuid(),
                Name = dto.Name.Trim(),
                Sku = string.IsNullOrWhiteSpace(dto.Sku) ? null : dto.Sku.Trim(),
                Unit = dto.Unit.Trim(),
                ReorderLevel = dto.ReorderLevel,
                UnitCost = dto.UnitCost,
                QuantityOnHand = dto.QuantityOnHand,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Add(item);

            if (dto.LinkToExistingProductId is Guid existingProductId)
            {
                var product = await _db.Products
                    .FirstOrDefaultAsync(p => p.Id == existingProductId, cancellationToken)
                    ?? throw new InvalidOperationException("Product to link was not found.");

                if (product.InventoryItemId is not null)
                {
                    throw new InvalidOperationException(
                        $"Product '{product.Name}' is already linked to another inventory item.");
                }

                product.InventoryItemId = item.Id;
                product.StockQuantity = (int)Math.Round(dto.QuantityOnHand);
                product.UpdatedAt = DateTime.UtcNow;
            }

            else if (dto.CreateAsProduct)
            {
                if (string.IsNullOrWhiteSpace(dto.ProductBrand) || dto.ProductSellingPrice is null)
                {
                    throw new InvalidOperationException(
                        "ProductBrand and ProductSellingPrice are required when CreateAsProduct is true.");
                }

                if (dto.ProductServiceId is Guid serviceId)
                {
                    var serviceExists = await _db.Services.AnyAsync(s => s.Id == serviceId, cancellationToken);
                    if (!serviceExists)
                    {
                        throw new InvalidOperationException("The specified Service was not found.");
                    }
                }

                var newProduct = new Product
                {
                    Id = Guid.NewGuid(),
                    ServiceId = dto.ProductServiceId,
                    InventoryItemId = item.Id,
                    Brand = dto.ProductBrand.Trim(),
                    Name = item.Name,
                    PartNumber = string.IsNullOrWhiteSpace(dto.ProductPartNumber) ? null : dto.ProductPartNumber.Trim(),
                    CompatibleVehicleType = string.IsNullOrWhiteSpace(dto.ProductCompatibleVehicleType) ? null : dto.ProductCompatibleVehicleType.Trim(),
                    CostPrice = dto.UnitCost,
                    SellingPrice = dto.ProductSellingPrice.Value,
                    StockQuantity = (int)Math.Round(dto.QuantityOnHand),
                    Unit = item.Unit,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.Add(newProduct);
            }

            await _db.SaveChangesAsync(cancellationToken);

            return await GetItemByIdAsync(item.Id, cancellationToken);
        }

        public async Task AdjustStockAsync(Guid itemId, StockAdjustmentDto dto, bool isStockIn, CancellationToken cancellationToken)
        {
            var item = await _db.InventoryItems
                .FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken)
                ?? throw new InvalidOperationException("Inventory item was not found.");

            var signedQuantity = isStockIn ? dto.Quantity : -dto.Quantity;

            if (item.QuantityOnHand + signedQuantity < 0)
            {
                throw new InvalidOperationException(
                    $"Insufficient stock for '{item.Name}'. Available: {item.QuantityOnHand}, requested: {dto.Quantity}.");
            }

            item.QuantityOnHand += signedQuantity;
            item.UpdatedAt = DateTime.UtcNow;

            _db.Add(new InventoryItemTransaction
            {
                Id = Guid.NewGuid(),
                InventoryItemId = item.Id,
                Type = isStockIn ? InventoryTransactionType.StockIn : InventoryTransactionType.StockOut,
                Quantity = dto.Quantity,
                Note = dto.Note,
                CreatedBy = dto.UserId,
                CreatedAt = DateTime.UtcNow
            });


            var linkedProduct = await _db.Products
                .FirstOrDefaultAsync(p => p.InventoryItemId == item.Id, cancellationToken);

            if (linkedProduct is not null)
            {
                linkedProduct.StockQuantity = (int)Math.Round(item.QuantityOnHand);
                linkedProduct.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }


        private async Task<Dictionary<Guid, Product>> GetLinkedProductsByItemIdsAsync(
            IEnumerable<Guid> itemIds, CancellationToken cancellationToken)
        {
            var ids = itemIds.ToList();
            if (ids.Count == 0)
            {
                return new Dictionary<Guid, Product>();
            }

            var linkedProducts = await _db.Products
                .AsNoTracking()
                .Include(p => p.Service)
                    .ThenInclude(s => s!.Category)
                .Where(p => p.InventoryItemId != null && ids.Contains(p.InventoryItemId!.Value))
                .ToListAsync(cancellationToken);

            return linkedProducts.ToDictionary(p => p.InventoryItemId!.Value);
        }

        private static InventoryItemResponseDto MapToDto(InventoryItemEntity item, Product? linkedProduct)
        {
            return new InventoryItemResponseDto
            {
                Id = item.Id,
                Name = item.Name,
                Sku = item.Sku,
                Unit = item.Unit,
                QuantityOnHand = item.QuantityOnHand,
                ReorderLevel = item.ReorderLevel,
                UnitCost = item.UnitCost,
                IsActive = item.IsActive,
                LinkedProductId = linkedProduct?.Id,
                LinkedProductName = linkedProduct?.Name,
                LinkedProductStockQuantity = linkedProduct?.StockQuantity,
                LinkedServiceId = linkedProduct?.ServiceId,
                LinkedServiceName = linkedProduct?.Service?.Name,
                LinkedCategoryName = linkedProduct?.Service?.Category?.Name
            };
        }
    }
}
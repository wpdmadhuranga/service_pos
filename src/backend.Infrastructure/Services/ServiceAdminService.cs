using backend.Application.Common.Interfaces;
using backend.Application.DTOs.Core.Services;
using backend.Application.Services;
using backend.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using backend.Application.DTOs.Core.ServiceCategory;
using backend.Application.DTOs.Inventory.Products;

namespace backend.Infrastructure.Services
{
    public sealed class ServiceAdminService : IServiceAdminService
    {
        private readonly IApplicationDbContext _db;

        public ServiceAdminService(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<ServiceDto> CreateAsync(ServiceCreateRequest request, CancellationToken cancellationToken = default)
        {
            var category = await _db.ServiceCategories.FirstOrDefaultAsync(category => category.Id == request.CategoryId, cancellationToken)
                ?? throw new InvalidOperationException("Service category was not found.");

            var service = new backend.Domain.Entities.Service
            {
                Id = Guid.NewGuid(),
                CategoryId = category.Id,
                Name = request.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                DefaultPrice = request.DefaultPrice,
                PricingType = request.PricingType,
                MinPrice = request.PricingType == PricingType.Variable ? request.MinPrice : null,
                MaxPrice = request.PricingType == PricingType.Variable ? request.MaxPrice : null,
                Unit = string.IsNullOrWhiteSpace(request.Unit) ? null : request.Unit.Trim(),
                IsActive = request.IsActive,
                SortOrder = request.SortOrder
            };

            _db.Add(service);
            await _db.SaveChangesAsync(cancellationToken);

            return await LoadDtoAsync(service.Id, cancellationToken);
        }

        public async Task<ServiceDto> UpdateAsync(Guid id, ServiceUpdateRequest request, CancellationToken cancellationToken = default)
        {
            var service = await _db.Services
                .Include(item => item.Category)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
                ?? throw new InvalidOperationException("Service was not found.");

            if (request.CategoryId.HasValue)
            {
                var categoryExists = await _db.ServiceCategories.AnyAsync(category => category.Id == request.CategoryId.Value, cancellationToken);
                if (!categoryExists)
                {
                    throw new InvalidOperationException("Service category was not found.");
                }

                service.CategoryId = request.CategoryId.Value;
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                service.Name = request.Name.Trim();
            }

            if (request.Description is not null)
            {
                service.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            }

            if (request.DefaultPrice.HasValue)
            {
                service.DefaultPrice = request.DefaultPrice.Value;
            }

            if (request.PricingType.HasValue)
            {
                service.PricingType = request.PricingType.Value;
            }

            if (service.PricingType == PricingType.Fixed)
            {
                service.MinPrice = null;
                service.MaxPrice = null;
            }
            else
            {
                if (request.MinPrice.HasValue)
                {
                    service.MinPrice = request.MinPrice.Value;
                }

                if (request.MaxPrice.HasValue)
                {
                    service.MaxPrice = request.MaxPrice.Value;
                }

                if (service.MinPrice.HasValue && service.MaxPrice.HasValue && service.MinPrice.Value > service.MaxPrice.Value)
                {
                    throw new InvalidOperationException("MinPrice cannot be greater than MaxPrice.");
                }
            }

            if (request.Unit is not null)
            {
                service.Unit = string.IsNullOrWhiteSpace(request.Unit) ? null : request.Unit.Trim();
            }

            if (request.IsActive.HasValue)
            {
                service.IsActive = request.IsActive.Value;
            }

            if (request.SortOrder.HasValue)
            {
                service.SortOrder = request.SortOrder.Value;
            }

            await _db.SaveChangesAsync(cancellationToken);
            return await LoadDtoAsync(service.Id, cancellationToken);
        }

        private async Task<ServiceDto> LoadDtoAsync(Guid serviceId, CancellationToken cancellationToken)
        {
            var service = await _db.Services
                .AsNoTracking()
                .Include(item => item.Category)
                .FirstAsync(item => item.Id == serviceId, cancellationToken);

            return new ServiceDto(
                service.Id,
                service.CategoryId,
                service.Category.Name,
                service.Name,
                service.Description,
                service.DefaultPrice,
                service.PricingType,
                service.MinPrice,
                service.MaxPrice,
                service.Unit,
                service.IsActive,
                service.SortOrder);
        }
    

    // public async Task<IEnumerable<ServiceDto>> GetAllAsync(CancellationToken cancellationToken = default)
    //     {
    //         var services = await _db.Services
    //             .AsNoTracking()
    //             .Include(item => item.Category)
    //             .ToListAsync(cancellationToken);

    //         return services.Select(service => new ServiceDto(
    //             service.Id,
    //             service.CategoryId,
    //             service.Category.Name,
    //             service.Name,
    //             service.Description,
    //             service.DefaultPrice,
    //             service.PricingType,
    //             service.MinPrice,
    //             service.MaxPrice,
    //             service.Unit,
    //             service.IsActive,
    //             service.SortOrder));
    //     } 

         public async Task<List<ServiceGetDto>> GetAllAsync()
        {
            var services = await _db.Services
                .AsNoTracking()
                .Include(s => s.Category)
                .Include(s => s.Products)
                .OrderBy(s => s.Category.SortOrder)
                .ThenBy(s => s.SortOrder)
                .Select(s => new ServiceGetDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    DefaultPrice = s.DefaultPrice,
                    PricingType = s.PricingType.ToString(),
                    MinPrice = s.MinPrice,
                    MaxPrice = s.MaxPrice,
                    Unit = s.Unit,
                    IsActive = s.IsActive,
                    SortOrder = s.SortOrder,
                    Category = new ServiceCategoryDto
                    {
                        Id = s.Category.Id,
                        Name = s.Category.Name,
                        SortOrder = s.Category.SortOrder
                    },
                    Products = s.Products.Select(p => new ProductDto
                    {
                        Id = p.Id,
                        Brand = p.Brand,
                        Name = p.Name,
                        PartNumber = p.PartNumber,
                        CompatibleVehicleType = p.CompatibleVehicleType,
                        CostPrice = p.CostPrice,
                        SellingPrice = p.SellingPrice,
                        StockQuantity = p.StockQuantity,
                        Unit = p.Unit,
                        IsActive = p.IsActive
                    }).ToList()
                })
                .ToListAsync();

            return services;
        }  
}}

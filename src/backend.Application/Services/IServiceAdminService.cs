using backend.Application.DTOs.Services;

namespace backend.Application.Services
{
    public interface IServiceAdminService
    {
        Task<ServiceDto> CreateAsync(ServiceCreateRequest request, CancellationToken cancellationToken = default);
        Task<ServiceDto> UpdateAsync(Guid id, ServiceUpdateRequest request, CancellationToken cancellationToken = default);
    }
}
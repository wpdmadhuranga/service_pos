using backend.Application.Common.Models;

namespace backend.Application.History
{
    public interface IHistoryService
    {
        Task<PagedResult<HistoryInvoiceListItemDto>> GetInvoicesAsync(HistoryInvoiceListQueryRequest request, CancellationToken cancellationToken = default);
        Task<HistoryInvoiceDetailDto> GetInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default);
        Task<PagedResult<HistoryInvoiceListItemDto>> GetDueInvoicesAsync(HistoryInvoiceListQueryRequest request, CancellationToken cancellationToken = default);
        Task<PagedResult<HistoryInvoiceListItemDto>> GetCustomerInvoicesAsync(Guid customerId, HistoryInvoiceListQueryRequest request, CancellationToken cancellationToken = default);
    }
}
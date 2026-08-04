namespace backend.Application.Pos
{
    public interface IPosService
    {
        Task<IReadOnlyList<PosServiceCategoryGroupDto>> GetActiveServicesAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<PosCustomerSearchResultDto>> SearchCustomersAsync(string query, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<PosVehicleDto>> GetCustomerVehiclesAsync(Guid customerId, CancellationToken cancellationToken = default);
        Task<PosInvoiceDetailDto> CreateDraftInvoiceAsync(PosCreateInvoiceRequest request, CancellationToken cancellationToken = default);
        Task<PosInvoiceDetailDto> UpdateDraftInvoiceAsync(Guid invoiceId, PosUpdateDraftInvoiceRequest request, CancellationToken cancellationToken = default);
        Task<PosInvoiceDetailDto> CompleteInvoiceAsync(Guid invoiceId, Guid userId, CancellationToken cancellationToken = default);
        Task<PosInvoiceDetailDto> RecordPaymentAsync(Guid invoiceId, PosRecordPaymentRequest request, CancellationToken cancellationToken = default);
        Task<PosInvoiceDetailDto> CancelInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default);
    }
}
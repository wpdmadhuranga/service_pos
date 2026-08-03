using backend.Application.Common.Interfaces;
using backend.Application.Common.Models;
using backend.Application.History;
using backend.Domain.Entities;
using backend.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.History.Service
{
    public sealed class HistoryService : IHistoryService
    {
        private readonly IApplicationDbContext _db;

        public HistoryService(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<PagedResult<HistoryInvoiceListItemDto>> GetInvoicesAsync(HistoryInvoiceListQueryRequest request, CancellationToken cancellationToken = default)
        {
            var query = BuildInvoiceQuery(request, dueOnly: false);
            return await ExecutePagedListAsync(query, request, cancellationToken);
        }

        public async Task<HistoryInvoiceDetailDto> GetInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default)
        {
            var invoice = await LoadInvoiceAsync(invoiceId, cancellationToken)
                ?? throw new InvalidOperationException("Invoice was not found.");

            return ToDetailDto(invoice);
        }

        public async Task<PagedResult<HistoryInvoiceListItemDto>> GetDueInvoicesAsync(HistoryInvoiceListQueryRequest request, CancellationToken cancellationToken = default)
        {
            var query = BuildInvoiceQuery(request, dueOnly: true);
            return await ExecutePagedListAsync(query, request, cancellationToken);
        }

        public async Task<PagedResult<HistoryInvoiceListItemDto>> GetCustomerInvoicesAsync(Guid customerId, HistoryInvoiceListQueryRequest request, CancellationToken cancellationToken = default)
        {
            var customerExists = await _db.Customers.AnyAsync(customer => customer.Id == customerId, cancellationToken);
            if (!customerExists)
            {
                throw new InvalidOperationException("Customer was not found.");
            }

            var adjustedRequest = request with { CustomerId = customerId };
            var query = BuildInvoiceQuery(adjustedRequest, dueOnly: false);
            return await ExecutePagedListAsync(query, adjustedRequest, cancellationToken);
        }

        private IQueryable<Invoice> BuildInvoiceQuery(HistoryInvoiceListQueryRequest request, bool dueOnly)
        {
            var query = _db.Invoices
                .AsNoTracking()
                .Include(invoice => invoice.Customer)
                .Include(invoice => invoice.Vehicle)
                .Include(invoice => invoice.Payments)
                .Include(invoice => invoice.InvoiceItems)
                .AsQueryable();

            if (request.CustomerId.HasValue)
            {
                query = query.Where(invoice => invoice.CustomerId == request.CustomerId.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                if (Enum.TryParse<InvoiceStatus>(request.Status, true, out var status))
                {
                    query = query.Where(invoice => invoice.Status == status);
                }
            }

            if (request.DateFrom.HasValue)
            {
                var from = request.DateFrom.Value;
                query = query.Where(invoice => invoice.CreatedAt >= from);
            }

            if (request.DateTo.HasValue)
            {
                var to = request.DateTo.Value;
                query = query.Where(invoice => invoice.CreatedAt <= to);
            }

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var term = request.Search.Trim().ToLowerInvariant();
                query = query.Where(invoice =>
                    invoice.InvoiceNumber.ToLower().Contains(term) ||
                    invoice.Customer.Name.ToLower().Contains(term) ||
                    invoice.Customer.Phone.ToLower().Contains(term) ||
                    invoice.Vehicle.PlateNumber.ToLower().Contains(term) ||
                    invoice.InvoiceItems.Any(item => item.NameSnapshot.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(request.PaymentStatus))
            {
                var paymentStatus = request.PaymentStatus.Trim().ToLowerInvariant();
                query = paymentStatus switch
                {
                    "unpaid" => query.Where(invoice => invoice.Payments.Sum(payment => payment.Amount) <= 0m),
                    "partial" => query.Where(invoice => invoice.Payments.Sum(payment => payment.Amount) > 0m && invoice.Payments.Sum(payment => payment.Amount) < invoice.Total),
                    "paid" => query.Where(invoice => invoice.Total <= 0m || invoice.Payments.Sum(payment => payment.Amount) >= invoice.Total),
                    _ => query
                };
            }

            if (dueOnly)
            {
                query = query.Where(invoice => invoice.Total > 0m && invoice.Payments.Sum(payment => payment.Amount) < invoice.Total);
            }

            query = ApplySort(query, request);
            return query;
        }

        private static IQueryable<Invoice> ApplySort(IQueryable<Invoice> query, HistoryInvoiceListQueryRequest request)
        {
            var sortBy = request.SortBy.Trim().ToLowerInvariant();
            var isDescending = request.SortDir.Trim().Equals("desc", StringComparison.OrdinalIgnoreCase);

            return (sortBy, isDescending) switch
            {
                ("invoicenumber", true) => query.OrderByDescending(invoice => invoice.InvoiceNumber),
                ("invoicenumber", false) => query.OrderBy(invoice => invoice.InvoiceNumber),
                ("customername", true) => query.OrderByDescending(invoice => invoice.Customer.Name).ThenByDescending(invoice => invoice.CreatedAt),
                ("customername", false) => query.OrderBy(invoice => invoice.Customer.Name).ThenBy(invoice => invoice.CreatedAt),
                ("total", true) => query.OrderByDescending(invoice => invoice.Total).ThenByDescending(invoice => invoice.CreatedAt),
                ("total", false) => query.OrderBy(invoice => invoice.Total).ThenBy(invoice => invoice.CreatedAt),
                ("status", true) => query.OrderByDescending(invoice => invoice.Status).ThenByDescending(invoice => invoice.CreatedAt),
                ("status", false) => query.OrderBy(invoice => invoice.Status).ThenBy(invoice => invoice.CreatedAt),
                (_, true) => query.OrderByDescending(invoice => invoice.CreatedAt),
                _ => query.OrderBy(invoice => invoice.CreatedAt)
            };
        }

        private async Task<PagedResult<HistoryInvoiceListItemDto>> ExecutePagedListAsync(
            IQueryable<Invoice> query,
            HistoryInvoiceListQueryRequest request,
            CancellationToken cancellationToken)
        {
            var totalCount = await query.CountAsync(cancellationToken);

            var invoices = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var items = invoices.Select(ToListItemDto).ToList();
            return new PagedResult<HistoryInvoiceListItemDto>(items, request.Page, request.PageSize, totalCount);
        }

        private async Task<Invoice?> LoadInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken)
        {
            return await _db.Invoices
                .AsNoTracking()
                .Include(invoice => invoice.Customer)
                .Include(invoice => invoice.Vehicle)
                .Include(invoice => invoice.Payments)
                .Include(invoice => invoice.InvoiceItems)
                .FirstOrDefaultAsync(invoice => invoice.Id == invoiceId, cancellationToken);
        }

        private static HistoryInvoiceListItemDto ToListItemDto(Invoice invoice)
        {
            var amountPaid = RoundMoney(invoice.Payments.Sum(payment => payment.Amount));
            var paymentStatus = GetPaymentStatus(invoice.Total, amountPaid);

            return new HistoryInvoiceListItemDto(
                invoice.Id,
                invoice.InvoiceNumber,
                invoice.Status.ToString(),
                paymentStatus,
                invoice.CustomerId,
                invoice.Customer.Name,
                invoice.Customer.Phone,
                invoice.VehicleId,
                invoice.Vehicle.PlateNumber,
                invoice.Subtotal,
                invoice.Discount,
                invoice.Tax,
                invoice.Total,
                amountPaid,
                RoundMoney(Math.Max(0m, invoice.Total - amountPaid)),
                invoice.OdometerAtService,
                invoice.Notes,
                invoice.CreatedAt,
                invoice.UpdatedAt);
        }

        private static HistoryInvoiceDetailDto ToDetailDto(Invoice invoice)
        {
            var amountPaid = RoundMoney(invoice.Payments.Sum(payment => payment.Amount));
            var paymentStatus = GetPaymentStatus(invoice.Total, amountPaid);

            return new HistoryInvoiceDetailDto(
                invoice.Id,
                invoice.InvoiceNumber,
                invoice.CustomerId,
                invoice.VehicleId,
                invoice.UserId,
                invoice.OdometerAtService,
                invoice.Status.ToString(),
                invoice.Subtotal,
                invoice.Discount,
                invoice.Tax,
                invoice.Total,
                amountPaid,
                paymentStatus,
                invoice.Notes,
                invoice.CreatedAt,
                invoice.UpdatedAt,
                new HistoryCustomerDto(
                    invoice.Customer.Id,
                    invoice.Customer.Name,
                    invoice.Customer.Phone,
                    invoice.Customer.Email,
                    invoice.Customer.Address),
                new HistoryVehicleDto(
                    invoice.Vehicle.Id,
                    invoice.Vehicle.PlateNumber,
                    invoice.Vehicle.Make,
                    invoice.Vehicle.Model,
                    invoice.Vehicle.Year,
                    invoice.Vehicle.VehicleType,
                    invoice.Vehicle.OdometerReading),
                invoice.InvoiceItems
                    .OrderBy(item => item.Id)
                    .Select(item => new HistoryInvoiceItemDto(
                        item.Id,
                        item.ServiceId,
                        item.NameSnapshot,
                        item.PriceSnapshot,
                        item.Quantity,
                        item.LineTotal))
                    .ToList(),
                invoice.Payments
                    .OrderBy(payment => payment.PaidAt)
                    .Select(payment => new HistoryPaymentDto(
                        payment.Id,
                        payment.Amount,
                        payment.Method,
                        payment.PaidAt,
                        payment.ReferenceNo))
                    .ToList());
        }

        private static decimal RoundMoney(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        private static string GetPaymentStatus(decimal total, decimal amountPaid)
        {
            if (total <= 0m)
            {
                return "Paid";
            }

            if (amountPaid <= 0m)
            {
                return "Unpaid";
            }

            if (amountPaid < total)
            {
                return "Partial";
            }

            return "Paid";
        }
    }
}
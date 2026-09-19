using System.Text.RegularExpressions;
using backend.Application.Common.Interfaces;
using backend.Application.Pos;
using backend.Domain.Entities;
using backend.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using backend.Domain.Entities.Audit;

namespace backend.Infrastructure.Pos.Service
{
    public sealed class PosService : IPosService
    {
        private const int CustomerSearchLimit = 20;
        private readonly IApplicationDbContext _db;
        private const int RecentInvoicesPerVehicle = 5;
        private const int RecentInvoicesWithoutVehicle = 5;
        public PosService(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<PosServiceCategoryGroupDto>> GetActiveServicesAsync(CancellationToken cancellationToken = default)
        {
            var services = await _db.Services
                .AsNoTracking()
                .Where(service => service.IsActive)
                .Include(service => service.Category)
                .OrderBy(service => service.Category.SortOrder)
                .ThenBy(service => service.Category.Name)
                .ThenBy(service => service.SortOrder)
                .ThenBy(service => service.Name)
                .ToListAsync(cancellationToken);

            var products = await _db.Products
                .AsNoTracking()
                .Where(product => product.IsActive)
                .OrderBy(product => product.Brand)
                .ThenBy(product => product.Name)
                .ToListAsync(cancellationToken);

            var productsByServiceId = products
                .Where(product => product.ServiceId.HasValue)
                .GroupBy(product => product.ServiceId!.Value)
                .ToDictionary(group => group.Key, group => group.ToList());

            return services
                .GroupBy(service => new { service.CategoryId, service.Category.Name, service.Category.SortOrder })
                .OrderBy(group => group.Key.SortOrder)
                .ThenBy(group => group.Key.Name)
                .Select(group => new PosServiceCategoryGroupDto(
                    group.Key.CategoryId,
                    group.Key.Name,
                    group.Key.SortOrder,
                    group.Select(service => new PosServiceDto(
                        service.Id,
                        service.Name,
                        service.Description,
                        service.DefaultPrice,
                        service.PricingType,
                        service.MinPrice,
                        service.MaxPrice,
                        service.Unit,
                        service.SortOrder,
                        productsByServiceId.TryGetValue(service.Id, out var serviceProducts)
                            ? serviceProducts.Select(product => new PosProductDto(
                                product.Id,
                                product.Brand,
                                product.Name,
                                product.SellingPrice,
                                product.StockQuantity)).ToList()
                            : [])).ToList()))
                .ToList();
        }

        public async Task<IReadOnlyList<PosCustomerSearchResultDto>> SearchCustomersAsync(string query, CancellationToken cancellationToken = default)
        {
            var term = query.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(term))
            {
                return Array.Empty<PosCustomerSearchResultDto>();
            }

            return await _db.Customers
                .AsNoTracking()
                .Where(customer => customer.Name.ToLower().Contains(term) || customer.Phone.ToLower().Contains(term))
                .OrderBy(customer => customer.Name)
                .Take(CustomerSearchLimit)
                .Select(customer => new PosCustomerSearchResultDto(
                    customer.Id,
                    customer.Name,
                    customer.Phone,
                    customer.Email,
                    customer.Address))
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<PosVehicleDto>> GetCustomerVehiclesAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            var customerExists = await _db.Customers.AnyAsync(customer => customer.Id == customerId, cancellationToken);
            if (!customerExists)
            {
                throw new InvalidOperationException("Customer was not found.");
            }

            return await _db.Vehicles
                .AsNoTracking()
                .Where(vehicle => vehicle.CustomerId == customerId)
                .OrderBy(vehicle => vehicle.PlateNumber)
                .Select(vehicle => new PosVehicleDto(
                    vehicle.Id,
                    vehicle.CustomerId,
                    vehicle.PlateNumber,
                    vehicle.Make,
                    vehicle.Model,
                    vehicle.Year,
                    vehicle.VehicleType,
                    vehicle.OdometerReading))
                .ToListAsync(cancellationToken);
        }

        public async Task<PosInvoiceDetailDto> CreateDraftInvoiceAsync(
            PosCreateInvoiceRequest request,
            CancellationToken cancellationToken = default)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(item => item.Id == request.UserId, cancellationToken)
                ?? throw new InvalidOperationException("User was not found.");

            var (customer, vehicle) = await ResolveCustomerAndVehicleAsync(request, cancellationToken);
            var invoiceItems = await BuildInvoiceItemsAsync(request.Items, cancellationToken);
            var invoiceNumber = await GenerateInvoiceNumberAsync(cancellationToken);

            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = invoiceNumber,
                CustomerId = customer?.Id,
                VehicleId = vehicle?.Id,
                UserId = user.Id,
                OdometerAtService = request.OdometerAtService,
                Status = InvoiceStatus.Completed,
                Discount = 0m,
                Tax = 0m,
                Notes = request.Notes,
                InvoiceItems = invoiceItems,
                AmountPaid = 0
            };

            ApplyTotals(invoice);
            ValidateSoftStock(invoice.InvoiceItems);

            foreach (var invoiceItem in invoice.InvoiceItems)
            {
                if (invoiceItem.ProductId.HasValue)
                {
                    var product = await _db.Products
                        .Include(p => p.InventoryItem)
                        .FirstOrDefaultAsync(p => p.Id == invoiceItem.ProductId.Value, cancellationToken);

                    if (product != null)
                    {
                        product.StockQuantity -= invoiceItem.Quantity;
                        product.UpdatedAt = DateTime.UtcNow;

                        if (product.InventoryItemId.HasValue)
                        {
                            var inventoryItem = await _db.InventoryItems
                                .FirstOrDefaultAsync(i => i.Id == product.InventoryItemId.Value, cancellationToken);

                            if (inventoryItem != null)
                            {
                                inventoryItem.QuantityOnHand -= invoiceItem.Quantity;
                                inventoryItem.UpdatedAt = DateTime.UtcNow;

                                _db.Add(new backend.Domain.Entities.Inventory.InvoiceItemInventoryUsage
                                {
                                    Id = Guid.NewGuid(),
                                    InvoiceItemId = invoiceItem.Id,
                                    InventoryItemId = inventoryItem.Id,
                                    QuantityUsed = invoiceItem.Quantity
                                });
                            }
                        }
                    }
                }
            }

            if (request.InitialPayment is not null)
            {
                if (request.InitialPayment.Amount > invoice.Total)
                {
                    throw new InvalidOperationException("Payment amount exceeds invoice total.");
                }

                invoice.AmountPaid = request.InitialPayment.Amount;
                invoice.PaymentStatus = invoice.AmountPaid >= invoice.Total ? PaymentStatus.Paid : PaymentStatus.PartiallyPaid;

                invoice.Payments.Add(new Payment
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoice.Id,
                    Amount = request.InitialPayment.Amount,
                    Method = request.InitialPayment.Method,
                    PaidAt = request.InitialPayment.PaidAt ?? DateTime.UtcNow,
                    ReferenceNo = request.InitialPayment.ReferenceNo
                });
            }
            else
            {
                invoice.PaymentStatus = PaymentStatus.Unpaid;
            }

            _db.Add(invoice);
            await _db.SaveChangesAsync(cancellationToken);

            return await LoadInvoiceDetailAsync(invoice.Id, cancellationToken);
        }
        public async Task<PosInvoiceDetailDto> UpdateDraftInvoiceAsync(Guid invoiceId, PosUpdateDraftInvoiceRequest request, CancellationToken cancellationToken = default)
        {
            var invoice = await LoadInvoiceForEditAsync(invoiceId, cancellationToken)
                ?? throw new InvalidOperationException("Invoice was not found.");

            if (invoice.Status != InvoiceStatus.Draft)
            {
                throw new InvalidOperationException("Only draft invoices can be updated.");
            }

            if (request.Discount.HasValue)
            {
                invoice.Discount = request.Discount.Value;
            }

            if (request.Notes is not null)
            {
                invoice.Notes = request.Notes;
            }

            if (request.Items is not null)
            {
                invoice.InvoiceItems.Clear();
                var items = await BuildInvoiceItemsAsync(request.Items, cancellationToken);
                foreach (var item in items)
                {
                    invoice.InvoiceItems.Add(item);
                }
            }

            ApplyTotals(invoice);
            ValidateSoftStock(invoice.InvoiceItems);
            await _db.SaveChangesAsync(cancellationToken);

            return await LoadInvoiceDetailAsync(invoice.Id, cancellationToken);
        }

        public async Task<PosInvoiceDetailDto> CompleteInvoiceAsync(Guid invoiceId, Guid userId, CancellationToken cancellationToken = default)
        {
            var invoice = await LoadInvoiceForEditAsync(invoiceId, cancellationToken)
                ?? throw new InvalidOperationException("Invoice was not found.");

            if (invoice.Status != InvoiceStatus.Draft)
            {
                throw new InvalidOperationException("Only draft invoices can be completed.");
            }

            ApplyTotals(invoice);
            var stockIssues = await ValidateAndApplyStockOutAsync(invoice, userId, cancellationToken);
            if (stockIssues.Count > 0)
            {
                throw new InvalidOperationException($"Insufficient stock: {string.Join(", ", stockIssues)}");
            }

            invoice.Status = InvoiceStatus.Completed;
            if (invoice.Vehicle is not null && invoice.OdometerAtService.HasValue && invoice.OdometerAtService.Value > invoice.Vehicle.OdometerReading)
            {
                invoice.Vehicle.OdometerReading = invoice.OdometerAtService.Value;
            }

            await _db.SaveChangesAsync(cancellationToken);

            return await LoadInvoiceDetailAsync(invoice.Id, cancellationToken);
        }

        public async Task<PosInvoiceDetailDto> RecordPaymentAsync(Guid invoiceId, PosRecordPaymentRequest request, CancellationToken cancellationToken)
        {
            var invoice = await _db.Invoices
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken)
                ?? throw new InvalidOperationException("Invoice was not found.");

            if (invoice.Status == InvoiceStatus.Cancelled)
                throw new InvalidOperationException("Cannot record payment on a cancelled invoice.");

            if (invoice.Status == InvoiceStatus.Draft)
                throw new InvalidOperationException("Invoice must be completed before recording payment.");

            if (request.Amount <= 0)
                throw new InvalidOperationException("Payment amount must be greater than zero.");

            var totalAlreadyPaid = invoice.Payments.Sum(p => p.Amount);
            var remainingDue = invoice.Total - totalAlreadyPaid;

            if (request.Amount > remainingDue)
            {
                throw new InvalidOperationException($"Payment amount ({request.Amount:C}) exceeds remaining due ({remainingDue:C}).");
            }

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoice.Id,
                Amount = request.Amount,
                Method = request.Method,
                PaidAt = DateTime.UtcNow,
                ReferenceNo = request.ReferenceNo
            };

            _db.Add(payment);

            invoice.AmountPaid = totalAlreadyPaid + request.Amount;

            if (invoice.AmountPaid >= invoice.Total)
            {
                invoice.PaymentStatus = PaymentStatus.Paid;
            }
            else if (invoice.AmountPaid > 0)
            {
                invoice.PaymentStatus = PaymentStatus.PartiallyPaid;
            }
            else
            {
                invoice.PaymentStatus = PaymentStatus.Unpaid;
            }

            invoice.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return await LoadInvoiceDetailAsync(invoice.Id, cancellationToken);
        }

        public async Task<PosInvoiceDetailDto> CancelInvoiceAsync(
    Guid invoiceId,
    CancellationToken cancellationToken = default)
        {
            var invoice = await LoadInvoiceForEditAsync(invoiceId, cancellationToken)
                ?? throw new InvalidOperationException("Invoice was not found.");

            if (invoice.Status is not InvoiceStatus.Draft and not InvoiceStatus.Completed)
            {
                throw new InvalidOperationException(
                    "Only draft or completed invoices can be cancelled.");
            }

            if (invoice.Status == InvoiceStatus.Cancelled)
            {
                throw new InvalidOperationException(
                    "This invoice is already cancelled.");
            }

            var now = DateTime.UtcNow;

            var invoiceItems = invoice.InvoiceItems.ToList();


            foreach (var invoiceItem in invoiceItems)
            {
                if (!invoiceItem.ProductId.HasValue)
                {
                    continue;
                }

                var product = await _db.Products
                    .FirstOrDefaultAsync(
                        p => p.Id == invoiceItem.ProductId.Value,
                        cancellationToken);

                if (product == null)
                {
                    throw new InvalidOperationException(
                        $"Product {invoiceItem.ProductId.Value} was not found.");
                }

                product.StockQuantity += invoiceItem.Quantity;
                product.UpdatedAt = now;
            }


            var invoiceItemIds = invoiceItems
                .Select(ii => ii.Id)
                .ToList();

            var usages = await _db.InvoiceItemInventoryUsages
                .Where(u => invoiceItemIds.Contains(u.InvoiceItemId))
                .ToListAsync(cancellationToken);


            foreach (var usage in usages)
            {
                var inventoryItem = await _db.InventoryItems
                    .FirstOrDefaultAsync(
                        i => i.Id == usage.InventoryItemId,
                        cancellationToken);

                if (inventoryItem == null)
                {
                    throw new InvalidOperationException(
                        $"Inventory item {usage.InventoryItemId} was not found.");
                }

                inventoryItem.QuantityOnHand += usage.QuantityUsed;
                inventoryItem.UpdatedAt = now;
            }
            var oldValues = JsonSerializer.Serialize(new
            {
                invoice.Status,
                invoice.AmountPaid,
                invoice.PaymentStatus
            });

            invoice.Status = InvoiceStatus.Cancelled;
            invoice.UpdatedAt = now;

            var newValues = JsonSerializer.Serialize(new
            {
                invoice.Status,
                invoice.AmountPaid,
                invoice.PaymentStatus
            });

            _db.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                TableName = "Invoices",
                RecordId = invoice.Id,
                Action = "Cancel",
                ChangedAt = now,
                OldValues = oldValues,
                NewValues = newValues
            });


            await _db.SaveChangesAsync(cancellationToken);

            return await LoadInvoiceDetailAsync(
                invoice.Id,
                cancellationToken);
        }
        private async Task<(Customer? Customer, Vehicle? Vehicle)> ResolveCustomerAndVehicleAsync(PosCreateInvoiceRequest request, CancellationToken cancellationToken)
        {
            Customer? customer = null;

            if (request.CustomerId.HasValue)
            {
                customer = await _db.Customers.FirstOrDefaultAsync(item => item.Id == request.CustomerId.Value, cancellationToken)
                    ?? throw new InvalidOperationException("Customer was not found.");
            }
            else if (request.Customer is not null)
            {
                var trimmedPhone = request.Customer.Phone.Trim();

                customer = await _db.Customers.FirstOrDefaultAsync(item => item.Phone == trimmedPhone, cancellationToken);

                if (customer is null)
                {
                    customer = new Customer
                    {
                        Id = Guid.NewGuid(),
                        Name = request.Customer.Name.Trim(),
                        Phone = trimmedPhone,
                        Email = string.IsNullOrWhiteSpace(request.Customer.Email) ? null : request.Customer.Email.Trim(),
                        Address = string.IsNullOrWhiteSpace(request.Customer.Address) ? null : request.Customer.Address.Trim(),
                        Notes = string.IsNullOrWhiteSpace(request.Customer.Notes) ? null : request.Customer.Notes.Trim()
                    };

                    _db.Add(customer);
                }
            }

            Vehicle? vehicle = null;

            if (request.VehicleId.HasValue)
            {
                vehicle = await _db.Vehicles.FirstOrDefaultAsync(item => item.Id == request.VehicleId.Value, cancellationToken)
                    ?? throw new InvalidOperationException("Vehicle was not found.");
            }

            if (vehicle is not null && customer is not null && vehicle.CustomerId != customer.Id)
            {
                throw new InvalidOperationException("The selected vehicle does not belong to the selected customer.");
            }

            if (vehicle is null && request.Vehicle is not null)
            {
                var normalizedPlateNumber = request.Vehicle.PlateNumber.Trim();

                vehicle = await _db.Vehicles.FirstOrDefaultAsync(item => item.PlateNumber == normalizedPlateNumber, cancellationToken);

                if (vehicle is not null)
                {
                    if (customer is not null && vehicle.CustomerId != customer.Id)
                    {
                        throw new InvalidOperationException("This vehicle is already registered under a different customer.");
                    }
                }
                else
                {
                    if (customer is null)
                    {
                        throw new InvalidOperationException("A customer must be available before creating a new vehicle.");
                    }

                    vehicle = new Vehicle
                    {
                        Id = Guid.NewGuid(),
                        CustomerId = customer.Id,
                        PlateNumber = normalizedPlateNumber,
                        Make = string.IsNullOrWhiteSpace(request.Vehicle.Make) ? null : request.Vehicle.Make.Trim(),
                        Model = string.IsNullOrWhiteSpace(request.Vehicle.Model) ? null : request.Vehicle.Model.Trim(),
                        Year = request.Vehicle.Year,
                        VehicleType = string.IsNullOrWhiteSpace(request.Vehicle.VehicleType) ? null : request.Vehicle.VehicleType.Trim(),
                        OdometerReading = request.Vehicle.OdometerReading ?? 0
                    };

                    _db.Add(vehicle);
                }
            }

            if (vehicle is not null && customer is null)
            {
                customer = await _db.Customers.FirstOrDefaultAsync(item => item.Id == vehicle.CustomerId, cancellationToken);
            }

            if (vehicle is not null && customer is not null && vehicle.CustomerId != customer.Id)
            {
                throw new InvalidOperationException("The selected vehicle does not belong to the selected customer.");
            }

            return (customer, vehicle);
        }

        private async Task<List<InvoiceItem>> BuildInvoiceItemsAsync(IEnumerable<PosInvoiceItemInput> requestItems, CancellationToken cancellationToken)
        {
            var items = new List<InvoiceItem>();

            foreach (var requestItem in requestItems)
            {
                string nameSnapshot;
                string? brandSnapshot = null;
                decimal priceSnapshot;
                Guid? serviceId = requestItem.ServiceId;
                Guid? productId = requestItem.ProductId;

                if (productId.HasValue)
                {
                    var product = await _db.Products
                        .Include(product => product.Service)
                        .FirstOrDefaultAsync(item => item.Id == productId.Value && item.IsActive, cancellationToken)
                        ?? throw new InvalidOperationException("One or more selected products were not found or are inactive.");

                    if (serviceId.HasValue && product.ServiceId.HasValue && product.ServiceId.Value != serviceId.Value)
                    {
                        throw new InvalidOperationException($"Product '{product.Brand} {product.Name}' does not belong to the selected service.");
                    }

                    if (product.StockQuantity < requestItem.Quantity)
                    {
                        throw new InvalidOperationException($"Insufficient stock for product '{product.Brand} {product.Name}'.");
                    }

                    serviceId ??= product.ServiceId;

                    nameSnapshot = product.Name;
                    brandSnapshot = product.Brand;

                    if (requestItem.Price.HasValue)
                    {
                        priceSnapshot = requestItem.Price.Value;

                        if (priceSnapshot < 0m)
                        {
                            throw new InvalidOperationException($"Price cannot be negative for product '{product.Brand} {product.Name}'.");
                        }
                    }
                    else
                    {
                        priceSnapshot = product.SellingPrice;
                    }
                }
                else if (serviceId.HasValue)
                {
                    var service = await _db.Services.FirstOrDefaultAsync(item => item.Id == serviceId.Value && item.IsActive, cancellationToken)
                        ?? throw new InvalidOperationException("One or more selected services were not found or are inactive.");

                    nameSnapshot = service.Name;

                    if (service.PricingType == PricingType.Fixed)
                    {
                        priceSnapshot = service.DefaultPrice;
                    }
                    else
                    {
                        if (!requestItem.Price.HasValue)
                        {
                            throw new InvalidOperationException($"Price is required for variable service '{service.Name}'.");
                        }

                        priceSnapshot = requestItem.Price.Value;

                        if (priceSnapshot <= 0m)
                        {
                            throw new InvalidOperationException($"Price must be greater than zero for variable service '{service.Name}'.");
                        }

                        if (service.MinPrice.HasValue && priceSnapshot < service.MinPrice.Value)
                        {
                            throw new InvalidOperationException($"Price for '{service.Name}' is below the minimum allowed value.");
                        }

                        if (service.MaxPrice.HasValue && priceSnapshot > service.MaxPrice.Value)
                        {
                            throw new InvalidOperationException($"Price for '{service.Name}' is above the maximum allowed value.");
                        }
                    }
                }
                else
                {
                    nameSnapshot = requestItem.Name!.Trim();
                    priceSnapshot = requestItem.Price!.Value;

                    if (priceSnapshot <= 0m)
                    {
                        throw new InvalidOperationException("Price must be greater than zero for custom line items.");
                    }
                }

                items.Add(new InvoiceItem
                {
                    Id = Guid.NewGuid(),
                    ServiceId = serviceId,
                    ProductId = productId,
                    BrandSnapshot = brandSnapshot,
                    NameSnapshot = nameSnapshot,
                    PriceSnapshot = priceSnapshot,
                    Quantity = requestItem.Quantity,
                    LineTotal = RoundMoney(priceSnapshot * requestItem.Quantity)
                });
            }

            return items;
        }

        private async Task<List<string>> ValidateAndApplyStockOutAsync(Invoice invoice, Guid userId, CancellationToken cancellationToken)
        {
            var issues = new List<string>();
            var productRequests = invoice.InvoiceItems
                .Where(item => item.ProductId.HasValue)
                .GroupBy(item => item.ProductId!.Value)
                .Select(group => new { ProductId = group.Key, Quantity = group.Sum(item => item.Quantity) })
                .ToList();

            if (productRequests.Count == 0)
            {
                return issues;
            }

            var productIds = productRequests.Select(item => item.ProductId).ToList();
            var products = await _db.Products
                .Where(product => productIds.Contains(product.Id))
                .ToListAsync(cancellationToken);

            foreach (var request in productRequests)
            {
                var product = products.FirstOrDefault(item => item.Id == request.ProductId);
                if (product is null || product.StockQuantity < request.Quantity)
                {
                    issues.Add(product is null
                        ? request.ProductId.ToString()
                        : $"{product.Brand} {product.Name} (requested {request.Quantity}, available {product.StockQuantity})");
                }
            }

            if (issues.Count > 0)
            {
                return issues;
            }

            foreach (var request in productRequests)
            {
                var product = products.First(item => item.Id == request.ProductId);
                product.StockQuantity -= request.Quantity;

                _db.Add(new InventoryTransaction
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    Type = InventoryTransactionType.StockOut,
                    Quantity = request.Quantity,
                    InvoiceId = invoice.Id,
                    UserId = userId,
                    Notes = $"Stock out from invoice {invoice.InvoiceNumber}"
                });
            }

            return issues;
        }

        private static void ApplyTotals(Invoice invoice)
        {
            foreach (var item in invoice.InvoiceItems)
            {
                item.LineTotal = RoundMoney(item.PriceSnapshot * item.Quantity);
            }

            invoice.Subtotal = RoundMoney(invoice.InvoiceItems.Sum(item => item.LineTotal));
            invoice.Discount = RoundMoney(invoice.Discount);
            invoice.Tax = RoundMoney(invoice.Tax);
            invoice.Total = RoundMoney(invoice.Subtotal - invoice.Discount + invoice.Tax);
        }

        private async Task<string> GenerateInvoiceNumberAsync(CancellationToken cancellationToken)
        {
            var invoiceNumbers = await _db.Invoices
                .AsNoTracking()
                .Select(invoice => invoice.InvoiceNumber)
                .ToListAsync(cancellationToken);

            var highestNumber = invoiceNumbers
                .Select(invoiceNumber => Regex.Match(invoiceNumber ?? string.Empty, @"(\d+)$"))
                .Where(match => match.Success)
                .Select(match => int.Parse(match.Groups[1].Value))
                .DefaultIfEmpty(0)
                .Max();

            return $"INV-{highestNumber + 1:0000}";
        }

        private async Task<Invoice?> LoadInvoiceForEditAsync(Guid invoiceId, CancellationToken cancellationToken)
        {
            return await _db.Invoices
                .Include(invoice => invoice.Customer)
                .Include(invoice => invoice.Vehicle)
                .Include(invoice => invoice.InvoiceItems)
                .Include(invoice => invoice.Payments)
                .FirstOrDefaultAsync(invoice => invoice.Id == invoiceId, cancellationToken);
        }

        private async Task<PosInvoiceDetailDto> LoadInvoiceDetailAsync(Guid invoiceId, CancellationToken cancellationToken)
        {
            var invoice = await _db.Invoices
                .AsNoTracking()
                .Include(item => item.Customer)
                .Include(item => item.Vehicle)
                .Include(item => item.InvoiceItems)
                .Include(item => item.Payments)
                .FirstOrDefaultAsync(item => item.Id == invoiceId, cancellationToken)
                ?? throw new InvalidOperationException("Invoice was not found.");

            return ToDetailDto(invoice);
        }


        private static PosInvoiceDetailDto ToDetailDto(Invoice invoice)
        {
            var amountPaid = RoundMoney(invoice.Payments.Sum(payment => payment.Amount));
            var paymentStatus = GetPaymentStatus(invoice.Total, amountPaid);

            return new PosInvoiceDetailDto(
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
                invoice.Customer is null ? null : new PosInvoiceCustomerDto(
                invoice.Customer.Id,
                invoice.Customer.Name,
                invoice.Customer.Phone,
                invoice.Customer.Email,
                invoice.Customer.Address),
                invoice.Vehicle is null ? null : new PosInvoiceVehicleDto(
                invoice.Vehicle.Id,
                invoice.Vehicle.PlateNumber,
                invoice.Vehicle.Make,
                invoice.Vehicle.Model,
                invoice.Vehicle.Year,
                invoice.Vehicle.VehicleType,
                invoice.Vehicle.OdometerReading),
                invoice.InvoiceItems
                    .OrderBy(item => item.Id)
                    .Select(item => new PosInvoiceItemDto(
                        item.Id,
                        item.ServiceId,
                        item.ProductId,
                        item.BrandSnapshot,
                        item.NameSnapshot,
                        item.PriceSnapshot,
                        item.Quantity,
                        item.LineTotal))
                    .ToList(),
                invoice.Payments
                    .OrderBy(payment => payment.PaidAt)
                    .Select(payment => new PosPaymentDto(
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

        private static DateTime NormalizeDateTime(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }


        public async Task<PosDashboardInvoicesResponse> GetInvoiceOverviewAsync(
    CancellationToken cancellationToken = default)
        {
            var utcNow = DateTime.UtcNow;
            var todayStart = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Utc);
            var todayEnd = todayStart.AddDays(1);

            var todayInvoicesList = await _db.Invoices
                .AsNoTracking()
                .Include(i => i.Customer)
                .Include(i => i.Vehicle)
                .Include(i => i.InvoiceItems)
                .Include(i => i.Payments)
                .Where(i => i.Status == InvoiceStatus.Completed && i.CreatedAt >= todayStart && i.CreatedAt < todayEnd)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync(cancellationToken);

            var todayDtos = todayInvoicesList.Select(ToDetailDto).ToList();
            var todayRevenue = todayInvoicesList.Sum(i => i.Total);

            var weekStart = todayStart.AddDays(-(int)todayStart.DayOfWeek);
            var weekEnd = weekStart.AddDays(7);

            var weeklyRevenueRaw = await _db.Invoices
                .AsNoTracking()
                .Where(i => i.Status == InvoiceStatus.Completed && i.CreatedAt >= weekStart && i.CreatedAt < weekEnd)
                .GroupBy(i => i.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Revenue = g.Sum(i => i.Total) })
                .ToListAsync(cancellationToken);

            var weeklyRevenueByDay = Enumerable.Range(0, 7)
                .Select(offset => weekStart.AddDays(offset))
                .Select(date => new DailyRevenueDto(
                    date,
                    weeklyRevenueRaw.FirstOrDefault(x => x.Date == date)?.Revenue ?? 0m))
                .ToList();

            var weeklyRevenue = weeklyRevenueByDay.Sum(d => d.Revenue);

            var monthStart = new DateTime(utcNow.Year, utcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthEnd = monthStart.AddMonths(1);

            var monthlyRevenue = await _db.Invoices
                .AsNoTracking()
                .Where(i => i.Status == InvoiceStatus.Completed && i.CreatedAt >= monthStart && i.CreatedAt < monthEnd)
                .SumAsync(i => i.Total, cancellationToken);

            var duePaymentsList = await _db.Invoices
                .AsNoTracking()
                .Include(i => i.Customer)
                .Include(i => i.Vehicle)
                .Include(i => i.InvoiceItems)
                .Include(i => i.Payments)
                .Where(i => i.Status == InvoiceStatus.Completed && ((int)i.PaymentStatus == 0 || (int)i.PaymentStatus == 1))
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync(cancellationToken);

            var dueDtos = duePaymentsList.Select(ToDetailDto).ToList();
            var duePaymentsRevenue = duePaymentsList.Sum(i => i.Total);

            return new PosDashboardInvoicesResponse(
                todayDtos,
                todayRevenue,
                weeklyRevenue,
                weeklyRevenueByDay,
                monthlyRevenue,
                dueDtos,
                duePaymentsRevenue
            );
        }

        public async Task<PosInvoiceDetailDto> UpdateInvoicePaymentAsync(
            Guid invoiceId,
            PosRecordPaymentRequest request,
            CancellationToken cancellationToken = default)
        {
            var invoice = await _db.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Vehicle)
                .Include(i => i.InvoiceItems)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken)
                ?? throw new InvalidOperationException("Invoice was not found.");

            if (invoice.Status == InvoiceStatus.Cancelled)
            {
                throw new InvalidOperationException("Cannot record payment on a cancelled invoice.");
            }

            if (invoice.Status == InvoiceStatus.Draft)
            {
                throw new InvalidOperationException("Invoice must be completed before recording payment.");
            }

            if (request.Amount <= 0)
            {
                throw new InvalidOperationException("Payment amount must be greater than zero.");
            }

            var totalAlreadyPaid = invoice.Payments.Sum(p => p.Amount);
            var remainingDue = invoice.Total - totalAlreadyPaid;

            if (request.Amount > remainingDue)
            {
                throw new InvalidOperationException($"Payment amount ({request.Amount:C}) exceeds remaining due ({remainingDue:C}).");
            }

            var oldValues = JsonSerializer.Serialize(new
            {
                invoice.AmountPaid,
                invoice.PaymentStatus
            });

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoice.Id,
                Amount = request.Amount,
                Method = request.Method,
                PaidAt = DateTime.UtcNow,
                ReferenceNo = request.ReferenceNo
            };
            _db.Add(payment);

            invoice.AmountPaid = totalAlreadyPaid + request.Amount;

            if (invoice.AmountPaid >= invoice.Total)
            {
                invoice.PaymentStatus = PaymentStatus.Paid;
            }
            else if (invoice.AmountPaid > 0)
            {
                invoice.PaymentStatus = PaymentStatus.PartiallyPaid;
            }
            else
            {
                invoice.PaymentStatus = PaymentStatus.Unpaid;
            }

            invoice.UpdatedAt = DateTime.UtcNow;

            var newValues = JsonSerializer.Serialize(new
            {
                invoice.AmountPaid,
                invoice.PaymentStatus
            });

            _db.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                TableName = "Invoices",
                RecordId = invoice.Id,
                Action = "RecordPayment",
                ChangedBy = null,
                ChangedAt = DateTime.UtcNow,
                OldValues = oldValues,
                NewValues = newValues
            });

            await _db.SaveChangesAsync(cancellationToken);
            return ToDetailDto(invoice);
        }

        public async Task<PagedResultDto<PosCustomerDetailDto>> GetAllCustomersDetailAsync(
    int page,
    int pageSize,
    CancellationToken cancellationToken = default)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : pageSize;

            var totalCount = await _db.Customers.AsNoTracking().CountAsync(cancellationToken);

            var pagedCustomerIds = await _db.Customers
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);

            var customers = await _db.Customers
                .AsNoTracking()
                .Where(c => pagedCustomerIds.Contains(c.Id))
                .ToListAsync(cancellationToken);

            customers = pagedCustomerIds
                .Select(id => customers.First(c => c.Id == id))
                .ToList();

            var vehicles = await _db.Vehicles
                .AsNoTracking()
                .Where(v => pagedCustomerIds.Contains(v.CustomerId))
                .OrderBy(v => v.PlateNumber)
                .Select(v => new
                {
                    v.Id,
                    v.CustomerId,
                    v.PlateNumber,
                    v.Make,
                    v.Model,
                    v.Year,
                    v.VehicleType,
                    v.OdometerReading,
                    TotalInvoiceCount = v.Invoices.Count(),
                    RecentInvoices = v.Invoices
                        .OrderByDescending(i => i.CreatedAt)
                        .Take(RecentInvoicesPerVehicle)
                        .Select(i => new
                        {
                            i.Id,
                            i.InvoiceNumber,
                            Status = i.Status.ToString(),
                            i.Total,
                            i.Notes,
                            i.CreatedAt,
                            AmountPaid = i.Payments.Sum(p => p.Amount),
                            Items = i.InvoiceItems
                                .Select(it => new PosInvoiceItemsDto(
                                    it.Id, it.NameSnapshot, it.Quantity, it.PriceSnapshot, it.LineTotal))
                                .ToList()
                        })
                        .ToList()
                })
                .ToListAsync(cancellationToken);

            var noVehicleCounts = await _db.Invoices
                .AsNoTracking()
                .Where(i => i.CustomerId.HasValue
                    && pagedCustomerIds.Contains(i.CustomerId.Value)
                    && i.VehicleId == null)
                .GroupBy(i => i.CustomerId!.Value)
                .Select(g => new { CustomerId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.CustomerId, g => g.Count, cancellationToken);

            var recentNoVehicleByCustomer = new Dictionary<Guid, List<Invoice>>();
            foreach (var customerId in pagedCustomerIds)
            {
                var recent = await _db.Invoices
                    .AsNoTracking()
                    .Include(i => i.Payments)
                    .Include(i => i.InvoiceItems)
                    .Where(i => i.CustomerId == customerId && i.VehicleId == null)
                    .OrderByDescending(i => i.CreatedAt)
                    .Take(RecentInvoicesWithoutVehicle)
                    .ToListAsync(cancellationToken);

                if (recent.Count > 0)
                {
                    recentNoVehicleByCustomer[customerId] = recent;
                }
            }

            var vehiclesByCustomerId = vehicles
                .GroupBy(v => v.CustomerId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var items = customers.Select(customer =>
            {
                var customerVehicles = vehiclesByCustomerId.TryGetValue(customer.Id, out var vList)
                    ? vList
                    : [];

                var vehicleDtos = customerVehicles.Select(v => new PosVehicleWithInvoicesDto(
                    v.Id, v.PlateNumber, v.Make, v.Model, v.Year, v.VehicleType, v.OdometerReading,
                    v.TotalInvoiceCount,
                    v.RecentInvoices.Select(i => new PosInvoiceSummaryDto(
                        i.Id, i.InvoiceNumber, i.Status, i.Total,
                        RoundMoney(i.AmountPaid),
                        GetPaymentStatus(i.Total, RoundMoney(i.AmountPaid)),
                        i.Notes, i.CreatedAt, i.Items)).ToList()
                )).ToList();

                var noVehicleInvoices = recentNoVehicleByCustomer.TryGetValue(customer.Id, out var nvInvoices)
                    ? nvInvoices.Select(ToInvoiceSummaryDto).ToList()
                    : [];

                var noVehicleCount = noVehicleCounts.TryGetValue(customer.Id, out var nvCount) ? nvCount : 0;

                return new PosCustomerDetailDto(
                    customer.Id, customer.Name, customer.Phone, customer.Email,
                    customer.Address, customer.Notes,
                    vehicleDtos, noVehicleCount, noVehicleInvoices);
            }).ToList();

            return new PagedResultDto<PosCustomerDetailDto>(
                items, totalCount, page, pageSize,
                (int)Math.Ceiling(totalCount / (double)pageSize));
        }

        private static PosInvoiceSummaryDto ToInvoiceSummaryDto(Invoice invoice)
        {
            var amountPaid = RoundMoney(invoice.Payments.Sum(payment => payment.Amount));
            var itemDtos = invoice.InvoiceItems
                .Select(item => new PosInvoiceItemsDto(
                    item.Id, item.NameSnapshot, item.Quantity, item.PriceSnapshot, item.LineTotal))
                .ToList();

            return new PosInvoiceSummaryDto(
                invoice.Id, invoice.InvoiceNumber, invoice.Status.ToString(), invoice.Total,
                amountPaid, GetPaymentStatus(invoice.Total, amountPaid), invoice.Notes,
                invoice.CreatedAt, itemDtos);
        }

        //     private static PosCustomerDetailDto ToCustomerDetailDto(
        // Customer customer,
        // List<Invoice> invoicesWithoutVehicle)
        //     {
        //         return new PosCustomerDetailDto(
        //             customer.Id,
        //             customer.Name,
        //             customer.Phone,
        //             customer.Email,
        //             customer.Address,
        //             customer.Notes,
        //             customer.Vehicles
        //                 .OrderBy(vehicle => vehicle.PlateNumber)
        //                 .Select(vehicle => new PosVehicleWithInvoicesDto(
        //                     vehicle.Id,
        //                     vehicle.PlateNumber,
        //                     vehicle.Make,
        //                     vehicle.Model,
        //                     vehicle.Year,
        //                     vehicle.VehicleType,
        //                     vehicle.OdometerReading,
        //                     vehicle.Invoices
        //                         .OrderByDescending(invoice => invoice.CreatedAt)
        //                         .Select(ToInvoiceSummaryDto)
        //                         .ToList()))
        //                 .ToList(),
        //             invoicesWithoutVehicle
        //                 .Select(ToInvoiceSummaryDto)
        //                 .ToList());
        //     }

        //     private static PosInvoiceSummaryDto ToInvoiceSummaryDto(Invoice invoice)
        //     {
        //         var amountPaid = RoundMoney(invoice.Payments.Sum(payment => payment.Amount));

        //         var itemDtos = invoice.InvoiceItems
        //             .Select(item => new PosInvoiceItemsDto(
        //                 item.Id,
        //                 item.NameSnapshot,
        //                 item.Quantity,
        //                 item.PriceSnapshot,
        //                 item.LineTotal))
        //             .ToList();

        //         return new PosInvoiceSummaryDto(
        //             invoice.Id,
        //             invoice.InvoiceNumber,
        //             invoice.Status.ToString(),
        //             invoice.Total,
        //             amountPaid,
        //             GetPaymentStatus(invoice.Total, amountPaid),
        //             invoice.Notes,
        //             invoice.CreatedAt,
        //             itemDtos);
        //     }
        public async Task<IReadOnlyList<PosVehicleWithCustomerDto>> GetAllVehiclesWithCustomerAsync(CancellationToken cancellationToken = default)
        {
            return await _db.Vehicles
                .AsNoTracking()
                .Include(vehicle => vehicle.Customer)
                .OrderBy(vehicle => vehicle.PlateNumber)
                .Select(vehicle => new PosVehicleWithCustomerDto(
                    vehicle.Id,
                    vehicle.PlateNumber,
                    vehicle.Make,
                    vehicle.Model,
                    vehicle.Year,
                    vehicle.VehicleType,
                    vehicle.OdometerReading,
                    new PosVehicleCustomerDto(
                        vehicle.Customer.Id,
                        vehicle.Customer.Name,
                        vehicle.Customer.Phone,
                        vehicle.Customer.Email,
                        vehicle.Customer.Address,
                        vehicle.Customer.Notes)))
                .ToListAsync(cancellationToken);
        }


        private void ValidateSoftStock(IEnumerable<InvoiceItem> items)
        {
            var productRequests = items
                .Where(item => item.ProductId.HasValue)
                .GroupBy(item => item.ProductId!.Value)
                .Select(group => new { ProductId = group.Key, Quantity = group.Sum(item => item.Quantity) })
                .ToList();

            if (productRequests.Count == 0)
            {
                return;
            }

            var productIds = productRequests.Select(item => item.ProductId).ToList();
            var products = _db.Products
                .Where(product => productIds.Contains(product.Id))
                .ToList();

            var issues = new List<string>();
            foreach (var request in productRequests)
            {
                var product = products.FirstOrDefault(item => item.Id == request.ProductId);
                if (product is null || product.StockQuantity < request.Quantity)
                {
                    issues.Add(product is null
                        ? request.ProductId.ToString()
                        : $"{product.Brand} {product.Name} (requested {request.Quantity}, available {product.StockQuantity})");
                }
            }

            if (issues.Count > 0)
            {
                throw new InvalidOperationException($"Insufficient stock: {string.Join(", ", issues)}");
            }
        }

        public async Task<PagedResultDto<PosInvoiceSummaryDto>> GetCustomerInvoicesPagedAsync(
    Guid customerId,
    Guid? vehicleId,
    bool onlyWithoutVehicle,
    int page,
    int pageSize,
    CancellationToken cancellationToken = default)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : pageSize;

            var customerExists = await _db.Customers.AnyAsync(c => c.Id == customerId, cancellationToken);
            if (!customerExists)
            {
                throw new InvalidOperationException("Customer was not found.");
            }

            var query = _db.Invoices
                .AsNoTracking()
                .Include(i => i.Payments)
                .Include(i => i.InvoiceItems)
                .Where(i => i.CustomerId == customerId);

            if (vehicleId.HasValue)
            {
                query = query.Where(i => i.VehicleId == vehicleId.Value);
            }
            else if (onlyWithoutVehicle)
            {
                query = query.Where(i => i.VehicleId == null);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var invoices = await query
                .OrderByDescending(i => i.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var items = invoices.Select(invoice =>
            {
                var amountPaid = RoundMoney(invoice.Payments.Sum(p => p.Amount));

                var itemDtos = invoice.InvoiceItems
                    .Select(item => new PosInvoiceItemsDto(
                        item.Id,
                        item.NameSnapshot,
                        item.Quantity,
                        item.PriceSnapshot,
                        item.LineTotal))
                    .ToList();

                return new PosInvoiceSummaryDto(
                    invoice.Id,
                    invoice.InvoiceNumber,
                    invoice.Status.ToString(),
                    invoice.Total,
                    amountPaid,
                    GetPaymentStatus(invoice.Total, amountPaid),
                    invoice.Notes,
                    invoice.CreatedAt,
                    itemDtos);
            }).ToList();

            return new PagedResultDto<PosInvoiceSummaryDto>(
                items,
                totalCount,
                page,
                pageSize,
                (int)Math.Ceiling(totalCount / (double)pageSize));
        }

        public async Task<PagedResultDto<PosInvoiceDetailDto>> SearchInvoicesAsync(
    string? customerName,
    string? plateNumber,
    DateTime? date,
    DateTime? fromDate,
    DateTime? toDate,
    int page,
    int pageSize,
    CancellationToken cancellationToken = default)
        {
            var query = _db.Invoices
                .AsNoTracking()
                .Include(i => i.Customer)
                .Include(i => i.Vehicle)
                .Include(i => i.InvoiceItems)
                .Include(i => i.Payments)
                .Where(i => i.Status == InvoiceStatus.Completed);

            if (!string.IsNullOrWhiteSpace(customerName))
            {
                var name = customerName.Trim().ToLower();
                query = query.Where(i =>
                    i.Customer != null &&
                    i.Customer.Name.ToLower().Contains(name));
            }

            if (!string.IsNullOrWhiteSpace(plateNumber))
            {
                var plate = plateNumber.Trim().ToLower();
                query = query.Where(i =>
                    i.Vehicle != null &&
                    i.Vehicle.PlateNumber.ToLower().Contains(plate));
            }

            if (date.HasValue)
            {

                var dayStart = date.Value.Date;
                var utcDayStart = new DateTime(dayStart.Year, dayStart.Month, dayStart.Day, 0, 0, 0, DateTimeKind.Utc);
                var utcDayEnd = utcDayStart.AddDays(1);

                query = query.Where(i => i.CreatedAt >= utcDayStart && i.CreatedAt < utcDayEnd);
            }
            else
            {
                if (fromDate.HasValue)
                {
                    var from = fromDate.Value.Date;
                    var utcFrom = new DateTime(from.Year, from.Month, from.Day, 0, 0, 0, DateTimeKind.Utc);
                    query = query.Where(i => i.CreatedAt >= utcFrom);
                }

                if (toDate.HasValue)
                {
                    var to = toDate.Value.Date;
                    var utcTo = new DateTime(to.Year, to.Month, to.Day, 0, 0, 0, DateTimeKind.Utc).AddDays(1);
                    query = query.Where(i => i.CreatedAt < utcTo);
                }
            }

            query = query.OrderByDescending(i => i.CreatedAt);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var dtos = items.Select(ToDetailDto).ToList();

            return new PagedResultDto<PosInvoiceDetailDto>(
                dtos,
                totalCount,
                page,
                pageSize,
                (int)Math.Ceiling(totalCount / (double)pageSize)
            );
        }

        public async Task<IReadOnlyList<PosCustomerWithVehiclesDto>>
     GetAllCustomersWithVehiclesAsync(
         CancellationToken cancellationToken = default)
        {
            var customers = await _db.Customers
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Phone,
                    c.Email,
                    c.Address,
                    c.Notes
                })
                .ToListAsync(cancellationToken);

            var customerIds = customers
                .Select(c => c.Id)
                .ToList();

            var vehicles = await _db.Vehicles
                .AsNoTracking()
                .Where(v => customerIds.Contains(v.CustomerId))
                .OrderBy(v => v.PlateNumber)
                .Select(v => new
                {
                    v.Id,
                    v.CustomerId,
                    v.PlateNumber,
                    v.Make,
                    v.Model,
                    v.Year,
                    v.VehicleType,
                    v.OdometerReading
                })
                .ToListAsync(cancellationToken);

            var vehiclesByCustomer = vehicles
                .GroupBy(v => v.CustomerId)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyList<PosCustomerVehicleDto>)
                        group.Select(v => new PosCustomerVehicleDto(
                            v.Id,
                            v.PlateNumber,
                            v.Make,
                            v.Model,
                            v.Year,
                            v.VehicleType,
                            v.OdometerReading
                        )).ToList()
                );

            return customers
                .Select(customer =>
                    new PosCustomerWithVehiclesDto(
                        customer.Id,
                        customer.Name,
                        customer.Phone,
                        customer.Email,
                        customer.Address,
                        customer.Notes,
                        vehiclesByCustomer.TryGetValue(
                            customer.Id,
                            out var customerVehicles)
                            ? customerVehicles
                            : Array.Empty<PosCustomerVehicleDto>()
                    ))
                .ToList();
        }
    }
}
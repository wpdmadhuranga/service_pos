using backend.Application.Pos;
using Microsoft.AspNetCore.Mvc;

namespace backend.API.Controllers
{
    [ApiController]
    [Route("api/pos")]
    public class PosController : ControllerBase
    {
        private readonly IPosService _posService;

        public PosController(IPosService posService)
        {
            _posService = posService;
        }

        [HttpGet("services")]
        public async Task<ActionResult<IReadOnlyList<PosServiceCategoryGroupDto>>> GetServices(CancellationToken cancellationToken)
        {
            var result = await _posService.GetActiveServicesAsync(cancellationToken);
            return Ok(result);
        }

        [HttpGet("customers/search")]
        public async Task<ActionResult<IReadOnlyList<PosCustomerSearchResultDto>>> SearchCustomers([FromQuery(Name = "q")] string q, CancellationToken cancellationToken)
        {
            var result = await _posService.SearchCustomersAsync(q ?? string.Empty, cancellationToken);
            return Ok(result);
        }

        [HttpGet("customers/{id:guid}/vehicles")]
        public async Task<ActionResult<IReadOnlyList<PosVehicleDto>>> GetCustomerVehicles(Guid id, CancellationToken cancellationToken)
        {
            return await HandleAsync(() => _posService.GetCustomerVehiclesAsync(id, cancellationToken));
        }

        [HttpPost("invoices")]
        public async Task<ActionResult<PosInvoiceDetailDto>> CreateInvoice([FromBody] PosCreateInvoiceRequest request, CancellationToken cancellationToken)
        {
            var result = await HandleAsync(() => _posService.CreateDraftInvoiceAsync(request, cancellationToken));
            return Created($"/api/history/invoices/{result.Value!.Id}", result.Value);
        }

        [HttpPatch("invoices/{id:guid}")]
        public async Task<ActionResult<PosInvoiceDetailDto>> UpdateDraftInvoice(Guid id, [FromBody] PosUpdateDraftInvoiceRequest request, CancellationToken cancellationToken)
        {
            return await HandleAsync(() => _posService.UpdateDraftInvoiceAsync(id, request, cancellationToken));
        }

        [HttpPost("invoices/{id:guid}/complete")]
        public async Task<ActionResult<PosInvoiceDetailDto>> CompleteInvoice(Guid id, CancellationToken cancellationToken)
        {
            return await HandleAsync(() => _posService.CompleteInvoiceAsync(id, cancellationToken));
        }

        [HttpPost("invoices/{id:guid}/payments")]
        public async Task<ActionResult<PosInvoiceDetailDto>> RecordPayment(Guid id, [FromBody] PosRecordPaymentRequest request, CancellationToken cancellationToken)
        {
            return await HandleAsync(() => _posService.RecordPaymentAsync(id, request, cancellationToken));
        }

        [HttpPost("invoices/{id:guid}/cancel")]
        public async Task<ActionResult<PosInvoiceDetailDto>> CancelInvoice(Guid id, CancellationToken cancellationToken)
        {
            return await HandleAsync(() => _posService.CancelInvoiceAsync(id, cancellationToken));
        }

        private async Task<ActionResult<T>> HandleAsync<T>(Func<Task<T>> action)
        {
            try
            {
                var result = await action();
                return Ok(result);
            }
            catch (InvalidOperationException exception)
            {
                return ToErrorResult(exception);
            }
        }

        private ActionResult ToErrorResult(Exception exception)
        {
            if (IsNotFoundMessage(exception.Message))
            {
                return NotFound(new { message = exception.Message });
            }

            return BadRequest(new { message = exception.Message });
        }

        private static bool IsNotFoundMessage(string message)
        {
            return message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                || message.Contains("was not found", StringComparison.OrdinalIgnoreCase);
        }
    }
}
using backend.Application.Common.Models;
using backend.Application.History;
using Microsoft.AspNetCore.Mvc;

namespace backend.API.Controllers
{
    [ApiController]
    [Route("api/history")]
    public class HistoryController : ControllerBase
    {
        private readonly IHistoryService _historyService;

        public HistoryController(IHistoryService historyService)
        {
            _historyService = historyService;
        }

        [HttpGet("invoices")]
        public async Task<ActionResult<PagedResult<HistoryInvoiceListItemDto>>> GetInvoices([FromQuery] HistoryInvoiceListQueryRequest request, CancellationToken cancellationToken)
        {
            return await HandleAsync(() => _historyService.GetInvoicesAsync(request, cancellationToken));
        }

        [HttpGet("invoices/{id:guid}")]
        public async Task<ActionResult<HistoryInvoiceDetailDto>> GetInvoice(Guid id, CancellationToken cancellationToken)
        {
            return await HandleAsync(() => _historyService.GetInvoiceAsync(id, cancellationToken));
        }

        [HttpGet("invoices/due")]
        public async Task<ActionResult<PagedResult<HistoryInvoiceListItemDto>>> GetDueInvoices([FromQuery] HistoryInvoiceListQueryRequest request, CancellationToken cancellationToken)
        {
            return await HandleAsync(() => _historyService.GetDueInvoicesAsync(request, cancellationToken));
        }

        [HttpGet("customers/{id:guid}/invoices")]
        public async Task<ActionResult<PagedResult<HistoryInvoiceListItemDto>>> GetCustomerInvoices(Guid id, [FromQuery] HistoryInvoiceListQueryRequest request, CancellationToken cancellationToken)
        {
            return await HandleAsync(() => _historyService.GetCustomerInvoicesAsync(id, request, cancellationToken));
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
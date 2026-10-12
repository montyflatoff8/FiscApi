using FiscApi.DTOs;
using FiscApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace FiscApi.Controllers;

/// <summary>
/// HTTP endpoints for transactions. Each action calls the service once and maps the result
/// to a status code. No file access or business rules here; unexpected errors are handled
/// by UseExceptionHandler in Program.cs, so there's no try/catch.
/// </summary>
[ApiController]
[Route("api/transactions")]
[Produces("application/json")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _service;

    public TransactionsController(ITransactionService service)
    {
        _service = service;
    }

    // GET /api/transactions?type=Expense&category=Groceries  (both filters optional)
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TransactionResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TransactionResponse>>> GetAll(
        [FromQuery] string? type,
        [FromQuery] string? category,
        CancellationToken cancellationToken)
    {
        var transactions = await _service.GetAllAsync(type, category, cancellationToken);
        return Ok(transactions);
    }

    // GET /api/transactions/5
    [HttpGet("{id:int}")]
    [ProducesResponseType<TransactionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var transaction = await _service.GetByIdAsync(id, cancellationToken);
        return transaction is null ? NotFound() : Ok(transaction);
    }

    // POST /api/transactions
    [HttpPost]
    [ProducesResponseType<TransactionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TransactionResponse>> Create(
        TransactionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request, cancellationToken);

        return result.Outcome switch
        {
            // 201 + Location header pointing at GET /api/transactions/{id}
            ServiceOutcome.Success => CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value),
            ServiceOutcome.Duplicate => DuplicateProblem(),
            _ => throw new InvalidOperationException($"Unexpected outcome: {result.Outcome}")
        };
    }

    // PUT /api/transactions/5
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        int id,
        TransactionRequest request,
        CancellationToken cancellationToken)
    {
        var outcome = await _service.UpdateAsync(id, request, cancellationToken);

        return outcome switch
        {
            ServiceOutcome.Success => NoContent(),
            ServiceOutcome.NotFound => NotFound(),
            ServiceOutcome.Duplicate => DuplicateProblem(),
            _ => throw new InvalidOperationException($"Unexpected outcome: {outcome}")
        };
    }

    // DELETE /api/transactions/5
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var outcome = await _service.DeleteAsync(id, cancellationToken);
        return outcome == ServiceOutcome.Success ? NoContent() : NotFound();
    }

    // 409 body for the duplicate-transaction business rule, in problem-details format.
    private ObjectResult DuplicateProblem() => Problem(
        title: "Duplicate transaction",
        detail: "A transaction with the same date, amount, and description already exists.",
        statusCode: StatusCodes.Status409Conflict);
}
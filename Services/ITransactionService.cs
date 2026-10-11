using FiscApi.DTOs;

namespace FiscApi.Services;

/// <summary>
/// Business operations for transactions. The controller depends on this interface only.
/// Works entirely in DTOs, so the controller never sees the Transaction model.
/// </summary>
public interface ITransactionService
{
    // Optional filters; null means "don't filter on this". Newest first.
    Task<IReadOnlyList<TransactionResponse>> GetAllAsync(string? type, string? category, CancellationToken cancellationToken);

    // Null if not found.
    Task<TransactionResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);

    // Success (with the created transaction) or Duplicate.
    Task<ServiceResult<TransactionResponse>> CreateAsync(TransactionRequest request, CancellationToken cancellationToken);

    // Success, NotFound, or Duplicate.
    Task<ServiceOutcome> UpdateAsync(int id, TransactionRequest request, CancellationToken cancellationToken);

    // Success or NotFound.
    Task<ServiceOutcome> DeleteAsync(int id, CancellationToken cancellationToken);
}
using FiscApi.DTOs;
using FiscApi.Models;
using FiscApi.Repositories;

namespace FiscApi.Services;

/// <summary>
/// Business rules for transactions. Depends on ITransactionRepository (never a concrete repository)
/// </summary>
public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _repository;

    public TransactionService(ITransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<TransactionResponse>> GetAllAsync(
        string? type, string? category, CancellationToken cancellationToken)
    {
        IEnumerable<Transaction> transactions = await _repository.GetAllAsync(cancellationToken);

        // Each filter is optional and case-insensitive ("expense" matches "Expense").
        if (!string.IsNullOrWhiteSpace(type))
        {
            transactions = transactions.Where(t =>
                string.Equals(t.Type, type.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            transactions = transactions.Where(t =>
                string.Equals(t.Category, category.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        // Newest first, same order as the FiscApp home page.
        return transactions
            .OrderByDescending(t => t.Date)
            .Select(t => t.ToResponse())
            .ToList();
    }

    public async Task<TransactionResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var transaction = await _repository.GetByIdAsync(id, cancellationToken);
        return transaction?.ToResponse();
    }

    public async Task<ServiceResult<TransactionResponse>> CreateAsync(
        TransactionRequest request, CancellationToken cancellationToken)
    {
        var transaction = request.ToModel();
        ApplyIncomeRule(transaction);

        // BUSINESS RULE: no duplicate (same date, amount, and description).
        var existing = await _repository.GetAllAsync(cancellationToken);
        if (IsDuplicate(existing, transaction, excludeId: null))
        {
            return ServiceResult<TransactionResponse>.Duplicate();
        }

        // Server-controlled field: set here, never from the request.
        transaction.CreatedAt = DateTime.UtcNow;

        var saved = await _repository.AddAsync(transaction, cancellationToken);
        return ServiceResult<TransactionResponse>.Success(saved.ToResponse());
    }

    public async Task<ServiceOutcome> UpdateAsync(
        int id, TransactionRequest request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetAllAsync(cancellationToken);

        // 404 takes priority over 409: you can't conflict with yourself if you don't exist.
        if (!existing.Any(t => t.Id == id))
        {
            return ServiceOutcome.NotFound;
        }

        var transaction = request.ToModel();
        ApplyIncomeRule(transaction);

        // Exclude this transaction itself, so saving it unchanged isn't flagged as a duplicate.
        if (IsDuplicate(existing, transaction, excludeId: id))
        {
            return ServiceOutcome.Duplicate;
        }

        var updated = await _repository.UpdateAsync(id, transaction, cancellationToken);
        return updated ? ServiceOutcome.Success : ServiceOutcome.NotFound;
    }

    public async Task<ServiceOutcome> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var deleted = await _repository.DeleteAsync(id, cancellationToken);
        return deleted ? ServiceOutcome.Success : ServiceOutcome.NotFound;
    }

    // ---- Business rules ----

    // Duplicate = same calendar day, same amount, same description (ignoring case/extra spaces).
    private static bool IsDuplicate(IEnumerable<Transaction> existing, Transaction candidate, int? excludeId)
    {
        return existing.Any(t =>
            t.Id != excludeId &&
            t.Date.Date == candidate.Date.Date &&
            t.Amount == candidate.Amount &&
            string.Equals(t.Description.Trim(), candidate.Description.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    // Matches FiscApp: income never has a budget category (budgets only track spending).
    private static void ApplyIncomeRule(Transaction transaction)
    {
        if (transaction.Type == "Income")
        {
            transaction.Category = null;
        }
    }
}
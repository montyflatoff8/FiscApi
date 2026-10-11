using FiscApi.Models;

namespace FiscApi.Repositories;

/// <summary>
/// Storage contract for transactions. The service depends on this interface only, so storage
/// can be swapped (JSON file now, SQL Server in Part 2) without changing any other layer.
/// </summary>
public interface ITransactionRepository
{
    Task<IReadOnlyList<Transaction>> GetAllAsync(CancellationToken cancellationToken);

    // Returns null if no transaction has this id.
    Task<Transaction?> GetByIdAsync(int id, CancellationToken cancellationToken);

    // Assigns the Id, saves, and returns the saved transaction.
    Task<Transaction> AddAsync(Transaction transaction, CancellationToken cancellationToken);

    // Returns false if no transaction has this id. Keeps the original CreatedAt.
    Task<bool> UpdateAsync(int id, Transaction transaction, CancellationToken cancellationToken);

    // Returns false if no transaction has this id.
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}

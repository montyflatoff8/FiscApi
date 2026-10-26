using FiscApi.Data;
using FiscApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FiscApi.Repositories;

/// <summary>
/// Stores transactions in SQL Server through EF Core. Same interface as
/// JsonTransactionRepository, so the service and controller don't change at all.
/// Registered as SCOPED because FiscDbContext is scoped (one per HTTP request).
/// </summary>
public class SqlTransactionRepository : ITransactionRepository
{
    private readonly FiscDbContext _context;

    public SqlTransactionRepository(FiscDbContext context)
    {
        _context = context;
    }

    // AsNoTracking: read-only queries, so EF doesn't need to watch these objects for changes.
    public async Task<IReadOnlyList<Transaction>> GetAllAsync(CancellationToken cancellationToken)
        => await _context.Transactions
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<Transaction?> GetByIdAsync(int id, CancellationToken cancellationToken)
        => await _context.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<Transaction> AddAsync(Transaction transaction, CancellationToken cancellationToken)
    {
        // Id = 0 tells SQL Server to generate it (IDENTITY column), instead of the JSON max+1 trick.
        transaction.Id = 0;
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync(cancellationToken);
        return transaction;
    }

    public async Task<bool> UpdateAsync(int id, Transaction transaction, CancellationToken cancellationToken)
    {
        // Tracked load: EF compares the old and new values and only UPDATEs what changed.
        var existing = await _context.Transactions.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        // Copy client-settable fields only. Id and CreatedAt stay as stored (same rule as JSON).
        existing.Date = transaction.Date;
        existing.Amount = transaction.Amount;
        existing.Type = transaction.Type;
        existing.Category = transaction.Category;
        existing.Description = transaction.Description;
        existing.Notes = transaction.Notes;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var transaction = await _context.Transactions.FindAsync([id], cancellationToken);
        if (transaction is null)
        {
            return false;
        }

        _context.Transactions.Remove(transaction);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

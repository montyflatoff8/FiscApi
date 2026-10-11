using System.Text.Json;
using FiscApi.Models;

namespace FiscApi.Repositories;

/// <summary>
/// Stores transactions in Data/transactions.json under the content root.
/// Registered as a SINGLETON so every request shares the same lock (see _lock).
/// </summary>
public class JsonTransactionRepository : ITransactionRepository
{
    private readonly string _filePath;

    // One lock for the whole file: only one request may read or write it at a time.
    private readonly SemaphoreSlim _lock = new(1, 1);

    // camelCase so the file matches the API's JSON; indented so it's readable.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public JsonTransactionRepository(IWebHostEnvironment environment)
    {
        var dataFolder = Path.Combine(environment.ContentRootPath, "Data");
        Directory.CreateDirectory(dataFolder);   // no-op if it already exists
        _filePath = Path.Combine(dataFolder, "transactions.json");
    }

    public async Task<IReadOnlyList<Transaction>> GetAllAsync(CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return await ReadAllAsync(cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<Transaction?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var transactions = await ReadAllAsync(cancellationToken);
            return transactions.FirstOrDefault(t => t.Id == id);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<Transaction> AddAsync(Transaction transaction, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var transactions = await ReadAllAsync(cancellationToken);

            // Id assigned inside the lock, so two simultaneous POSTs can't get the same id.
            transaction.Id = transactions.Count == 0 ? 1 : transactions.Max(t => t.Id) + 1;

            transactions.Add(transaction);
            await WriteAllAsync(transactions, cancellationToken);
            return transaction;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> UpdateAsync(int id, Transaction transaction, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var transactions = await ReadAllAsync(cancellationToken);
            var index = transactions.FindIndex(t => t.Id == id);
            if (index == -1)
            {
                return false;
            }

            // Server-controlled fields come from the stored record, never from the update.
            transaction.Id = id;
            transaction.CreatedAt = transactions[index].CreatedAt;

            transactions[index] = transaction;
            await WriteAllAsync(transactions, cancellationToken);
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var transactions = await ReadAllAsync(cancellationToken);
            var removed = transactions.RemoveAll(t => t.Id == id);
            if (removed == 0)
            {
                return false;
            }

            await WriteAllAsync(transactions, cancellationToken);
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    // ---- Private helpers: only called from inside the lock above. ----

    // A missing or empty file is treated as "no transactions yet", not an error.
    private async Task<List<Transaction>> ReadAllAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
        {
            return new List<Transaction>();
        }

        var json = await File.ReadAllTextAsync(_filePath, cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<Transaction>();
        }

        return JsonSerializer.Deserialize<List<Transaction>>(json, JsonOptions) ?? new List<Transaction>();
    }

    // Write to a temp file first, then swap it in. If anything fails mid-write,
    // the real file is untouched and only the .tmp file is affected.
    private async Task WriteAllAsync(List<Transaction> transactions, CancellationToken cancellationToken)
    {
        var tempPath = _filePath + ".tmp";

        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, transactions, JsonOptions, cancellationToken);
        }

        File.Move(tempPath, _filePath, overwrite: true);
    }
}

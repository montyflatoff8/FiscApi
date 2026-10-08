namespace FiscApi.Models;

/// <summary>
/// A stored transaction. Never sent to or accepted from clients directly — the API uses
/// TransactionRequest / TransactionResponse instead.
/// </summary>
public class Transaction
{
    // Server-controlled: assigned by the repository / service, never by the client.
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }

    // Client-supplied (via TransactionRequest).
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;   // "Income" or "Expense"
    public string? Category { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
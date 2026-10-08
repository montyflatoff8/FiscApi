namespace FiscApi.DTOs;

/// <summary>
/// What the API returns for a transaction, including the server-controlled Id and CreatedAt.
/// </summary>
public class TransactionResponse
{
    public int Id { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime Date { get; init; }
    public decimal Amount { get; init; }
    public string Type { get; init; } = string.Empty;
    public string? Category { get; init; }
    public string Description { get; init; } = string.Empty;
    public string? Notes { get; init; }
}
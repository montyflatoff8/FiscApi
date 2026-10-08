using System.ComponentModel.DataAnnotations;

namespace FiscApi.DTOs;

/// <summary>
/// Body for POST and PUT. Contains only client-settable fields, so extra fields like "id" or
/// "createdAt" in the request are ignored (over-posting protection). Validated automatically
/// by [ApiController] before the action runs; failures return 400.
/// </summary>
public class TransactionRequest
{
    // Nullable so [Required] can detect a missing value (a plain DateTime would silently default to 0001-01-01).
    [Required(ErrorMessage = "Date is required.")]
    public DateTime? Date { get; set; }

    // Nullable for the same reason (a plain decimal would silently default to 0).
    [Required(ErrorMessage = "Amount is required.")]
    [Range(0.01, 1_000_000, ErrorMessage = "Amount must be between 0.01 and 1,000,000.")]
    public decimal? Amount { get; set; }

    [Required(ErrorMessage = "Type is required.")]
    [RegularExpression("^(Income|Expense)$", ErrorMessage = "Type must be 'Income' or 'Expense'.")]
    public string? Type { get; set; }

    [StringLength(50, ErrorMessage = "Category can be at most 50 characters.")]
    public string? Category { get; set; }

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Description must be between 1 and 100 characters.")]
    public string? Description { get; set; }

    [StringLength(250, ErrorMessage = "Notes can be at most 250 characters.")]
    public string? Notes { get; set; }
}
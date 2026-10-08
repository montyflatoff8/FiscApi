using FiscApi.Models;

namespace FiscApi.DTOs;

/// <summary>
/// Converts between the API's DTOs and the stored Model, so no other layer has to.
/// </summary>
public static class TransactionMappings
{
    // Request -> Model. Id and CreatedAt are NOT set here; the server assigns them.
    // The "!" operators are safe: [ApiController] validation has already rejected
    // any request where these required values are missing.
    public static Transaction ToModel(this TransactionRequest request) => new()
    {
        Date = request.Date!.Value,
        Amount = request.Amount!.Value,
        Type = request.Type!,
        Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim(),
        Description = request.Description!.Trim(),
        Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
    };

    // Model -> Response.
    public static TransactionResponse ToResponse(this Transaction t) => new()
    {
        Id = t.Id,
        CreatedAt = t.CreatedAt,
        Date = t.Date,
        Amount = t.Amount,
        Type = t.Type,
        Category = t.Category,
        Description = t.Description,
        Notes = t.Notes
    };
}
namespace FiscApi.Services;

/// <summary>
/// What happened when the service ran. The controller maps each outcome to an HTTP status
/// code; the service itself never deals with HTTP.
/// </summary>
public enum ServiceOutcome
{
    Success,
    NotFound,
    Duplicate
}

/// <summary>
/// An outcome plus a value (only set on Success). Used by operations that return data,
/// like Create.
/// </summary>
public record ServiceResult<T>(ServiceOutcome Outcome, T? Value = default)
{
    public static ServiceResult<T> Success(T value) => new(ServiceOutcome.Success, value);
    public static ServiceResult<T> NotFound() => new(ServiceOutcome.NotFound);
    public static ServiceResult<T> Duplicate() => new(ServiceOutcome.Duplicate);
}
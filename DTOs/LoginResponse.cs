namespace FiscApi.DTOs;

/// <summary>What a successful login returns. ExpiresIn is in seconds.</summary>
public class LoginResponse
{
    public required string AccessToken { get; init; }
    public required string TokenType { get; init; }
    public required int ExpiresIn { get; init; }
}

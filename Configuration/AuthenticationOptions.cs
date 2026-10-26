namespace FiscApi.Configuration;

/// <summary>
/// Binds the "Authentication" section. Issuer/Audience/minutes are safe to commit in
/// appsettings.json; SigningKey and DemoUser:Password are secrets and live in User Secrets.
/// </summary>
public class AuthenticationOptions
{
    public const string SectionName = "Authentication";

    // false = writes are open (Development only, for the "auth off" Postman run).
    public bool Enabled { get; set; } = true;

    // Who made the token, and who it's meant for. The API rejects tokens where these don't match.
    public string Issuer { get; set; } = "FiscApi";
    public string Audience { get; set; } = "FiscApp";

    public int AccessTokenMinutes { get; set; } = 60;

    // Secret used to sign tokens (HMAC-SHA256). Anyone with this key could forge a token.
    public string SigningKey { get; set; } = string.Empty;

    public DemoUserOptions DemoUser { get; set; } = new();
}

/// <summary>The single demo account. No registration endpoint, no roles.</summary>
public class DemoUserOptions
{
    public string Username { get; set; } = "demo";
    public string Password { get; set; } = string.Empty;
}

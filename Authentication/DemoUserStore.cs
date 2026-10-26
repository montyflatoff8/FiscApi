using FiscApi.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace FiscApi.Authentication;

public class DemoUser
{
    public required string Username { get; init; }
    public required string PasswordHash { get; init; }
}

/// <summary>
/// Holds the demo account. The plain password comes from User Secrets and is hashed once at
/// startup; after that only the hash is kept in memory, and logins are checked against it
/// with PasswordHasher (the same check the class Horse API uses).
/// </summary>
public class DemoUserStore
{
    private readonly PasswordHasher<DemoUser> _passwordHasher = new();
    private readonly DemoUser? _user;

    public DemoUserStore(IOptions<AuthenticationOptions> options)
    {
        var demo = options.Value.DemoUser;
        if (string.IsNullOrWhiteSpace(demo.Password))
        {
            return;   // no password configured = nobody can log in
        }

        var placeholder = new DemoUser { Username = demo.Username, PasswordHash = string.Empty };
        _user = new DemoUser
        {
            Username = demo.Username,
            PasswordHash = _passwordHasher.HashPassword(placeholder, demo.Password)
        };
    }

    // Usernames are case-insensitive; passwords are not.
    public bool TryGetUser(string username, out DemoUser user)
    {
        user = _user!;
        return _user is not null && string.Equals(_user.Username, username, StringComparison.OrdinalIgnoreCase);
    }

    public bool VerifyPassword(DemoUser user, string password) =>
        _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password) != PasswordVerificationResult.Failed;
}

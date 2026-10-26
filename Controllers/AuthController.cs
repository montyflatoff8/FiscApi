using FiscApi.Authentication;
using FiscApi.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FiscApi.Controllers;

/// <summary>
/// Login: trade a username + password for a JWT. The client then sends that token as
/// "Authorization: Bearer &lt;token&gt;" on every POST/PUT/DELETE.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly DemoUserStore _userStore;
    private readonly JwtTokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(DemoUserStore userStore, JwtTokenService tokenService, ILogger<AuthController> logger)
    {
        _userStore = userStore;
        _tokenService = tokenService;
        _logger = logger;
    }

    // POST /api/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Login(LoginRequest request)
    {
        // Same 401 for "no such user" and "wrong password", so attackers can't discover usernames.
        if (!_userStore.TryGetUser(request.Username!, out var user) ||
            !_userStore.VerifyPassword(user, request.Password!))
        {
            // Log the username tried, NEVER the password (people often type a real password by mistake).
            _logger.LogWarning("Failed login for username {Username}", request.Username);
            return Unauthorized();
        }

        var (token, expiresIn) = _tokenService.GenerateToken(user);

        // The token is a credential too - log that one was issued, not the token itself.
        _logger.LogInformation("User {Username} logged in", user.Username);

        // Tokens are credentials: tell browsers/proxies not to cache this response.
        Response.Headers.CacheControl = "no-store";
        return Ok(new LoginResponse { AccessToken = token, TokenType = "Bearer", ExpiresIn = expiresIn });
    }

    // GET /api/auth/me  - handy for checking a token in Postman.
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Me() => Ok(new { username = User.Identity?.Name });
}

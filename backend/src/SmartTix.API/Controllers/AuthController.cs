using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SmartTix.Application.Common;
using SmartTix.Application.DTOs.Auth;
using SmartTix.Application.Interfaces.Services;

namespace SmartTix.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    // HttpOnly cookie holding the refresh token. Invisible to JavaScript by
    // design — XSS cannot read it. Only sent to /api/auth/* because of Path.
    private const string RefreshCookieName = "smarttix_refresh";
    private const string RefreshCookiePath = "/api/auth";
    private static readonly TimeSpan RefreshCookieLifetime = TimeSpan.FromDays(7); // matches DB expiry

    private readonly IAuthService _authService;
    private readonly IWebHostEnvironment _env;

    public AuthController(IAuthService authService, IWebHostEnvironment env)
    {
        _authService = authService;
        _env = env;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        AppendRefreshCookie(result.RefreshToken);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<AuthResponse>.Ok(result, "Registration successful"));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        AppendRefreshCookie(result.RefreshToken);
        return Ok(ApiResponse<AuthResponse>.Ok(result, "Login successful"));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Refresh()
    {
        // Token comes from the HttpOnly cookie — never from the body, so JS
        // can't be tricked into exfiltrating it.
        var refreshToken = Request.Cookies[RefreshCookieName];
        if (string.IsNullOrEmpty(refreshToken))
            throw new UnauthorizedAccessException("Missing refresh token");

        var result = await _authService.RefreshTokenAsync(refreshToken);
        AppendRefreshCookie(result.RefreshToken);
        return Ok(ApiResponse<AuthResponse>.Ok(result, "Token refreshed"));
    }

    [HttpPost("logout")]
    [Authorize]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Logout()
    {
        // Idempotent: revoke whatever the cookie holds, always clear it.
        var refreshToken = Request.Cookies[RefreshCookieName];
        if (!string.IsNullOrEmpty(refreshToken))
            await _authService.LogoutAsync(refreshToken);

        DeleteRefreshCookie();
        return Ok(ApiResponse.OkNoData("Logged out successfully"));
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        return Ok(ApiResponse<object>.Ok(new { userId, email, role }, "Authenticated"));
    }

    private void AppendRefreshCookie(string token)
    {
        Response.Cookies.Append(RefreshCookieName, token, new CookieOptions
        {
            HttpOnly = true,                    // JS cannot read it (XSS defense)
            SameSite = SameSiteMode.Lax,        // never sent on cross-site POSTs (CSRF defense)
            Secure = !_env.IsDevelopment(),     // https-only in prod; localhost dev is http
            Path = RefreshCookiePath,           // only travels to auth endpoints
            Expires = DateTimeOffset.UtcNow.Add(RefreshCookieLifetime)
        });
    }

    private void DeleteRefreshCookie()
    {
        Response.Cookies.Delete(RefreshCookieName, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = !_env.IsDevelopment(),
            Path = RefreshCookiePath
        });
    }
}

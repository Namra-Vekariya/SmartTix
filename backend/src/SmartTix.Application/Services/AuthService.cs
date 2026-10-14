using SmartTix.Application.DTOs.Auth;
using SmartTix.Application.Interfaces.Repositories;
using SmartTix.Application.Interfaces.Services;
using SmartTix.Domain.Entities;

namespace SmartTix.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher _passwordHasher;
    public AuthService(
        IUserRepository userRepository,
        IJwtService jwtService,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // 1. Check if email already exists
        var emailExists = await _userRepository.ExistsByEmailAsync(request.Email);
        if (emailExists)
            throw new InvalidOperationException("Email is already registered");

        // 2. Create user entity
        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = request.Email.ToLower().Trim(),
            Phone = request.Phone?.Trim(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = Domain.Enums.UserRole.Customer
        };

        // 3. Create refresh token
        var refreshToken = new RefreshToken
        {
            Token = _jwtService.GenerateRefreshToken(),
            User = user,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        // 4. Save both in one transaction
        await _userRepository.AddAsync(user);
        await _userRepository.AddRefreshTokenAsync(refreshToken);
        await _userRepository.SaveChangesAsync();

        // 5. Generate access token and return
        return BuildAuthResponse(user, refreshToken.Token);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        // 1. Find user by email
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user is null)
            throw new UnauthorizedAccessException("Invalid email or password");

        // 2. Check account is active
        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is deactivated");

        // 3. Verify password
        // BCrypt.Verify compares plain password against stored hash
        var passwordValid = _passwordHasher.Verify(request.Password, user.PasswordHash);
        if (!passwordValid)
            throw new UnauthorizedAccessException("Invalid email or password");
        // Note: same error message for wrong email OR wrong password
        // Never tell the user which one is wrong — security best practice

        // 4. Create new refresh token for this session
        var refreshToken = new RefreshToken
        {
            Token = _jwtService.GenerateRefreshToken(),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        await _userRepository.AddRefreshTokenAsync(refreshToken);
        await _userRepository.SaveChangesAsync();

        return BuildAuthResponse(user, refreshToken.Token);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    {
        // 1. Find token in DB — includes the related User
        var storedToken = await _userRepository.GetRefreshTokenAsync(refreshToken);

        // 2. Validate every condition — all give same generic error
        // Never tell the caller WHY the token is invalid
        if (storedToken is null || storedToken.IsRevoked || storedToken.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Invalid or expired refresh token");

        if (!storedToken.User.IsActive)
            throw new UnauthorizedAccessException("Invalid or expired refresh token");

        // 3. Generate new access token using the existing refresh token
        // We do NOT rotate the refresh token here — same token, new access token
        // Rotation can be added later in the parking lot
        var newAccessToken = _jwtService.GenerateAccessToken(storedToken.User);

        return new AuthResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = refreshToken, // same refresh token returned
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(60),
            User = MapToUserDto(storedToken.User)
        };
    }

    public async Task LogoutAsync(string refreshToken)
    {
        // Revoke the refresh token — user must login again after this
        // Access token remains valid until it expires naturally (max 60 min)
        // This is acceptable — for instant access token invalidation
        // we would need Redis blacklisting (parking lot feature)
        await _userRepository.RevokeRefreshTokenAsync(refreshToken);
        await _userRepository.SaveChangesAsync();
    }

    // Private helper — builds AuthResponse from user + token
    // Avoids duplicating this mapping in Register and Login
    private AuthResponse BuildAuthResponse(User user, string refreshToken)
    {
        return new AuthResponse
        {
            AccessToken = _jwtService.GenerateAccessToken(user),
            RefreshToken = refreshToken,
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(60),
            User = MapToUserDto(user)
        };
    }

    private static UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString()
        };
    }
}
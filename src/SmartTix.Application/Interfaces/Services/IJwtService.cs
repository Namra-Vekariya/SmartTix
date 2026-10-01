using SmartTix.Domain.Entities;

namespace SmartTix.Application.Interfaces.Services;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}
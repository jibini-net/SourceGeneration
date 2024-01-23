using Generated;
using System.Security.Claims;

namespace TestApp.Services;

public interface IJwtAuthService
{
    (string, DateTime) GenerateToken(string signingKey, int seconds, List<Claim> claims);

    void GenerateToken(Account.WithRoles account, out Account.WithSession session);

    int GetUserId(string token);

    Task<bool> WasTokenEverValidAsync(string token);

    Task<bool> IsRefreshTokenValidAsync(string refreshToken);
}

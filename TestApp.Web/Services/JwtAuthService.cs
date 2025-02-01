using Generated;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TestApp.Extensions;

namespace TestApp.Services;

public class JwtAuthService(
    IConfiguration config
    ) : IJwtAuthService
{
    public (string, DateTime) GenerateToken(string signingKey, int seconds, List<Claim> claims)
    {
        var expires = DateTime.UtcNow.AddSeconds(seconds);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var handler = new JwtSecurityTokenHandler();
        var token = handler.WriteToken(new JwtSecurityToken(claims: claims, expires: expires, signingCredentials: creds));
        return (token, expires);
    }

    public void GenerateToken(Account.WithRoles account, out Account.WithSession session)
    {
        session = account.ConvertTo<Account.WithSession>();
        var claims = new List<Claim>()
        {
            new(ClaimTypes.Name, account.Email),
            new("Id", account.Id.ToString())
        };

        var resetKey = config.GetValue<string>("Jwt:RefreshKey");
        var resetSeconds = config.GetValue<int>("Jwt:RefreshSeconds");
        (session.RefreshToken, session.RefreshExpiration) = GenerateToken(resetKey, resetSeconds, claims);

        foreach (var role in account.Permissions)
        {
            claims.Add(new(ClaimTypes.Role, role.Name));
        }

        var key = config.GetValue<string>("Jwt:Key");
        var lifespanSeconds = config.GetValue<int>("Jwt:LifespanSeconds");
        (session.SessionToken, session.SessionExpiration) = GenerateToken(key, lifespanSeconds, claims);
    }

    public int GetUserId(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var idStr = jwt.Claims.First((it) => it.Type == "Id").Value;
        return int.Parse(idStr);
    }

    public async Task<bool> WasTokenEverValidAsync(string token)
    {
        var signingKey = config.GetValue<string>("Jwt:Key");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var handler = new JwtSecurityTokenHandler();

        var v = await handler.ValidateTokenAsync(token, new()
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = false,
            IssuerSigningKey = key
        });
        return v.IsValid;
    }

    public async Task<bool> IsRefreshTokenValidAsync(string refreshToken)
    {
        var signingKey = config.GetValue<string>("Jwt:RefreshKey");
        var skew = config.GetValue<int>("Jwt:SkewSeconds");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var handler = new JwtSecurityTokenHandler();

        var v = await handler.ValidateTokenAsync(refreshToken, new()
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.FromSeconds(skew)
        });
        return v.IsValid;
    }
}

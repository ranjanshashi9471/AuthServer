using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthServer.Application.Abstractions.Security;
using AuthServer.Application.Abstractions.Security.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthServer.Infrastructure.Security.Jwt;

internal sealed class JwtProvider : IJwtProvider
{
    private static readonly JwtSecurityTokenHandler TokenHandler = new();

    private readonly JwtOptions _options;

    public JwtProvider(IOptions<JwtOptions> options)
    {
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.SecretKey))
            throw new InvalidOperationException(
                "JWT SecretKey is missing.");

        if (_options.SecretKey.Length < 32)
            throw new InvalidOperationException(
                "JWT SecretKey must be at least 32 characters.");

        if (string.IsNullOrWhiteSpace(_options.Issuer))
            throw new InvalidOperationException(
                "JWT Issuer is missing.");

        if (string.IsNullOrWhiteSpace(_options.Audience))
            throw new InvalidOperationException(
                "JWT Audience is missing.");

        if (_options.ExpirationInMinutes <= 0)
            throw new InvalidOperationException(
                "JWT ExpirationInMinutes must be greater than zero.");
    }

    public string GenerateToken(JwtUser user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_options.SecretKey));

        var credentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.ExpirationInMinutes),
            signingCredentials: credentials);

        return TokenHandler.WriteToken(token);
    }
}
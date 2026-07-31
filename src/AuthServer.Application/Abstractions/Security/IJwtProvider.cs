using AuthServer.Application.Abstractions.Security.Models;

namespace AuthServer.Application.Abstractions.Security;

public interface IJwtProvider
{
    string GenerateToken(JwtUser user);
}

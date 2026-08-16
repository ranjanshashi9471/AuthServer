using AuthServer.Application.Abstractions.Security.Models;
using AuthServer.Domain.ValueObjects.Identifiers;

namespace AuthServer.Application.Abstractions.Security;

public interface IPasswordResetTokenProvider
{
    PasswordResetTokenData Generate();

    string BuildToken(PasswordResetTokenData resetPasswordToken);

    bool TryParse(string token, out PasswordResetTokenId resetPasswordTokenId, out string secret);
}

using AuthServer.Domain.Common;
using AuthServer.Domain.Exceptions;
using AuthServer.Domain.ValueObjects.Identifiers;

namespace AuthServer.Domain.Entities;

public sealed class ClientRedirectUri : Entity<ClientRedirectUriId>
{
    public ClientId ClientId { get; private set; }
    public string Uri { get; private set; }

    private ClientRedirectUri(ClientRedirectUriId id, ClientId clientId, string uri)
        : base(id, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
    {
        ClientId = clientId;
        Uri = uri;
    }

    private ClientRedirectUri()
    {
        Uri = default!;
    }

    internal static ClientRedirectUri Create(ClientId clientId, string uriString)
    {
        ValidateUri(uriString);
        return new ClientRedirectUri(ClientRedirectUriId.New(), clientId, uriString);
    }

    public static void ValidateUri(string uriString)
    {
        if (string.IsNullOrWhiteSpace(uriString))
        {
            throw new ValidationException("Redirect URI cannot be empty.");
        }

        if (!System.Uri.TryCreate(uriString, UriKind.Absolute, out var uri))
        {
            throw new ValidationException(
                $"The redirect URI '{uriString}' is not a valid absolute URI."
            );
        }

        if (!string.IsNullOrEmpty(uri.Fragment))
        {
            throw new BusinessRuleViolationException(
                "Redirect URIs must not contain a fragment component."
            );
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new BusinessRuleViolationException(
                "Redirect URIs must not contain user information."
            );
        }

        if (uri.Scheme != System.Uri.UriSchemeHttps)
        {
            if (uri.Scheme == System.Uri.UriSchemeHttp)
            {
                // Defers to .NET's native loopback parsing (localhost, 127.0.0.1, [::1])
                if (!uri.IsLoopback)
                {
                    throw new BusinessRuleViolationException(
                        "HTTP redirect URIs are only permitted for loopback interfaces."
                    );
                }
            }
            else
            {
                throw new BusinessRuleViolationException(
                    $"The scheme '{uri.Scheme}' is not supported for redirect URIs."
                );
            }
        }
    }
}

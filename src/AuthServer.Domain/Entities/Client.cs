using AuthServer.Domain.Common;
using AuthServer.Domain.Enums;
using AuthServer.Domain.Exceptions;
using AuthServer.Domain.ValueObjects.Identifiers;

namespace AuthServer.Domain.Entities;

public sealed class Client : Entity<ClientId>
{
    public string ClientIdentifier { get; private set; }
    public string Name { get; private set; }
    public ClientType Type { get; private set; }
    public bool RequirePkce { get; private set; }
    public ClientStatus Status { get; private set; }

    private readonly List<ClientRedirectUri> _redirectUris = [];
    public IReadOnlyCollection<ClientRedirectUri> RedirectUris => _redirectUris.AsReadOnly();

    private Client(
        ClientId id,
        string clientIdentifier,
        string name,
        ClientType type,
        bool requirePkce
    )
        : base(id, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
    {
        ClientIdentifier = clientIdentifier;
        Name = name;
        Type = type;
        RequirePkce = requirePkce;
        Status = ClientStatus.Active;
    }

    private Client()
    {
        ClientIdentifier = default!;
        Name = default!;
    }

    public static Client CreatePublic(string name, IEnumerable<string> initialRedirectUris)
    {
        ValidateName(name);
        ArgumentNullException.ThrowIfNull(initialRedirectUris);

        var client = new Client(
            ClientId.New(),
            GenerateClientIdentifier(),
            name.Trim(),
            ClientType.Public,
            requirePkce: true
        );

        var uris = initialRedirectUris.ToList();
        foreach (var uri in uris)
        {
            client.AddRedirectUri(uri);
        }

        if (client.RedirectUris.Count == 0)
        {
            throw new BusinessRuleViolationException(
                "Public clients must register at least one redirect URI."
            );
        }

        return client;
    }

    public static Client CreateConfidential(string name)
    {
        ValidateName(name);
        return new Client(
            ClientId.New(),
            GenerateClientIdentifier(),
            name.Trim(),
            ClientType.Confidential,
            requirePkce: true
        );
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Client name cannot be empty.");
        }
    }

    private static string GenerateClientIdentifier() => Guid.NewGuid().ToString("N");

    public void AddRedirectUri(string uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        var cleanUri = uri.Trim();

        ClientRedirectUri.ValidateUri(cleanUri);

        if (!_redirectUris.Any(r => r.Uri == cleanUri))
        {
            _redirectUris.Add(ClientRedirectUri.Create(Id, cleanUri));
            Touch();
        }
    }

    public void RemoveRedirectUri(string uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        var cleanUri = uri.Trim();

        var target = _redirectUris.FirstOrDefault(r => r.Uri == cleanUri);
        if (target is not null)
        {
            _redirectUris.Remove(target);
            Touch();
        }
    }

    public void Disable()
    {
        if (Status != ClientStatus.Inactive)
        {
            Status = ClientStatus.Inactive;
            Touch();
        }
    }

    public void Enable()
    {
        if (Status != ClientStatus.Active)
        {
            Status = ClientStatus.Active;
            Touch();
        }
    }
}

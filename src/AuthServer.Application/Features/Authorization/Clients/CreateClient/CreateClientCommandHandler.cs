using AuthServer.Application.Abstractions.Persistence;
using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Contracts.Authorization.Client;
using AuthServer.Domain.Entities;
using AuthServer.Domain.Enums;

namespace AuthServer.Application.Features.Authorization.Clients.CreateClient;

internal sealed class CreateClientCommandHandler
    : ICommandHandler<CreateClientCommand, CreateClientResponse>
{
    private readonly IClientRepository _clientRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateClientCommandHandler(IClientRepository clientRepository, IUnitOfWork unitOfWork)
    {
        _clientRepository = clientRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateClientResponse> Handle(
        CreateClientCommand request,
        CancellationToken cancellationToken
    )
    {
        var uris = request.RedirectUris ?? [];

        var client =
            request.Type == ClientType.Public
                ? Client.CreatePublic(request.Name, uris)
                : Client.CreateConfidential(request.Name);

        if (request.Type == ClientType.Confidential)
        {
            foreach (var uri in uris)
            {
                client.AddRedirectUri(uri);
            }
        }

        _clientRepository.Add(client);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateClientResponse(
            client.ClientIdentifier,
            client.Name,
            client.Type.ToString()
        );
    }
}

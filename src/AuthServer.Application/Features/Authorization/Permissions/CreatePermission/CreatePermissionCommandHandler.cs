using AuthServer.Application.Abstractions.Persistence;
using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Domain.Entities;
using AuthServer.Domain.Exceptions;
using AuthServer.Domain.ValueObjects.Identifiers;

namespace AuthServer.Application.Features.Authorization.Permissions.CreatePermission;

internal sealed class CreatePermissionCommandHandler
    : ICommandHandler<CreatePermissionCommand, PermissionId>
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePermissionCommandHandler(
        IPermissionRepository permissionRepository,
        IUnitOfWork unitOfWork
    )
    {
        _permissionRepository = permissionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PermissionId> Handle(
        CreatePermissionCommand request,
        CancellationToken cancellationToken
    )
    {
        if (await _permissionRepository.IsNameDuplicateAsync(request.Name, cancellationToken))
        {
            throw new BusinessRuleViolationException(
                $"Permission '{request.Name}' already exists."
            );
        }

        var permission = Permission.Create(request.Name, request.Description);

        _permissionRepository.Add(permission);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return permission.Id;
    }
}

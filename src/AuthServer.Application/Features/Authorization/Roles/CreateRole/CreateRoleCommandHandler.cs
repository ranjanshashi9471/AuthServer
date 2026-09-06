using AuthServer.Application.Abstractions.Persistence;
using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Domain.Entities;
using AuthServer.Domain.Exceptions;
using AuthServer.Domain.ValueObjects.Identifiers;

namespace AuthServer.Application.Features.Authorization.Roles.CreateRole;

internal sealed class CreateRoleCommandHandler : ICommandHandler<CreateRoleCommand, RoleId>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateRoleCommandHandler(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IUnitOfWork unitOfWork
    )
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<RoleId> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        if (await _roleRepository.IsNameDuplicateAsync(request.Name, cancellationToken))
        {
            throw new BusinessRuleViolationException($"Role '{request.Name}' already exists.");
        }

        if (request.PermissionIds.Count > 0)
        {
            var existingIds = await _permissionRepository.GetExistingIdsAsync(
                request.PermissionIds,
                cancellationToken
            );

            var missingIds = request.PermissionIds.Except(existingIds).ToList();

            if (missingIds.Count > 0)
            {
                throw new NotFoundException(
                    $"One or more permissions were not found: "
                        + $"{string.Join(", ", missingIds.Select(id => id.Value))}"
                );
            }
        }

        var role = Role.Create(request.Name, request.Description);

        foreach (var permissionId in request.PermissionIds)
        {
            role.AddPermission(permissionId);
        }

        _roleRepository.Add(role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return role.Id;
    }
}

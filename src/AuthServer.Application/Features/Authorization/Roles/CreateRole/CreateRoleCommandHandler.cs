using AuthServer.Application.Abstractions.Persistence;
using AuthServer.Application.Abstractions.Security;
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
    private readonly ICurrentUser _currentUser;
    private readonly IPermissionService _permissionService;

    public CreateRoleCommandHandler(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IPermissionService permissionService
    )
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _permissionService = permissionService;
    }

    public async Task<RoleId> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        HashSet<PermissionId> existingPermissionIds = [];

        // 1. Validate requested permissions exist.
        if (request.PermissionIds.Count > 0)
        {
            existingPermissionIds = await _permissionRepository.GetExistingIdsAsync(
                request.PermissionIds,
                cancellationToken
            );

            var missingIds = request.PermissionIds.Except(existingPermissionIds).ToList();

            if (missingIds.Count > 0)
            {
                throw new NotFoundException(
                    "One or more permissions were not found: "
                        + string.Join(", ", missingIds.Select(id => id.Value))
                );
            }

            // 2. Strict possession delegation boundary.
            var userId = _currentUser.UserId;

            if (userId is null)
            {
                throw new ForbiddenException("Authentication context is invalid or missing.");
            }

            var callerPermissions = await _permissionService.GetPermissionIdsAsync(
                userId,
                cancellationToken
            );

            var unauthorizedPermissions = request.PermissionIds.Except(callerPermissions).ToList();

            if (unauthorizedPermissions.Count > 0)
            {
                throw new ForbiddenException(
                    "You are not authorized to grant one or more requested permissions."
                );
            }
        }

        // 3. Business rules.
        if (await _roleRepository.IsNameDuplicateAsync(request.Name, cancellationToken))
        {
            throw new BusinessRuleViolationException($"Role '{request.Name}' already exists.");
        }

        // 4. State mutation.
        var role = Role.Create(request.Name, request.Description);

        foreach (var permissionId in request.PermissionIds)
        {
            role.AddPermission(permissionId);
        }

        _roleRepository.Add(role);

        // 5. Persist atomically.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return role.Id;
    }
}

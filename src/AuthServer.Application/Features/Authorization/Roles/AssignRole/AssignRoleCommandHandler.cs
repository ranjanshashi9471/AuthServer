using AuthServer.Application.Abstractions.Persistence;
using AuthServer.Application.Abstractions.Security;
using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Domain.Exceptions;
using AuthServer.Domain.ValueObjects.Identifiers;

namespace AuthServer.Application.Features.Authorization.Roles.AssignRole;

internal sealed class AssignRoleCommandHandler : ICommandHandler<AssignRoleCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IPermissionService _permissionService;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public AssignRoleCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IPermissionService permissionService,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork
    )
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _permissionService = permissionService;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        // 1. Strict Possession Security Boundary
        var userId = _currentUser.UserId;
        if (userId is null)
        {
            throw new ForbiddenException("Authentication context is invalid or missing.");
        }

        var targetRolePermissions = await _permissionRepository.GetPermissionIdsForRoleAsync(
            request.RoleId,
            cancellationToken
        );

        if (targetRolePermissions.Count > 0)
        {
            var callerPermissions = await _permissionService.GetPermissionIdsAsync(
                userId,
                cancellationToken
            );
            var unauthorizedPermissions = targetRolePermissions.Except(callerPermissions).ToList();

            if (unauthorizedPermissions.Count > 0)
            {
                throw new ForbiddenException(
                    "You are not authorized to assign a role containing permissions you do not possess."
                );
            }
        }

        // 2. Business Rules
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            throw new NotFoundException($"User '{request.UserId.Value}' not found.");
        }

        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
        {
            throw new NotFoundException($"Role '{request.RoleId.Value}' not found.");
        }

        // 3. State Mutation
        user.AssignRole(request.RoleId);

        // No need for _userRepository.Update(user)!
        // EF Core tracks the 'user' object and knows we modified its UserRoles collection.
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

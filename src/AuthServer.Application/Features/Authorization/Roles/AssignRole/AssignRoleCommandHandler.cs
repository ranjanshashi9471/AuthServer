using AuthServer.Application.Abstractions.Persistence;
using AuthServer.Application.Messaging.Abstractions;
using AuthServer.Domain.Exceptions;

namespace AuthServer.Application.Features.Authorization.Roles.AssignRole;

internal sealed class AssignRoleCommandHandler : ICommandHandler<AssignRoleCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AssignRoleCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork
    )
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        var user =
            await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException($"User '{request.UserId}' was not found.");

        var role =
            await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken)
            ?? throw new NotFoundException($"Role '{request.RoleId}' was not found.");

        // Aggregate protects against duplicate assignment internally
        user.AssignRole(role.Id);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

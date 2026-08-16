namespace AuthServer.Application.Abstractions.Persistence;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

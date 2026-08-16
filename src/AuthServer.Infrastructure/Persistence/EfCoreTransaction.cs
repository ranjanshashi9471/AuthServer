using AuthServer.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace AuthServer.Infrastructure.Persistence;

internal sealed class EfCoreTransaction : ITransaction
{
    private readonly IDbContextTransaction _transaction;

    public EfCoreTransaction(IDbContextTransaction transaction)
    {
        _transaction = transaction;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        return _transaction.CommitAsync(cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return _transaction.DisposeAsync();
    }
}

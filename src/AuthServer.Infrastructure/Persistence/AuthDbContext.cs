using AuthServer.Application.Abstractions.Persistence;
using AuthServer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Infrastructure.Persistence;

internal sealed class AuthDbContext : DbContext, IUnitOfWork
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();

    public async Task<ITransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default
    )
    {
        var transaction = await Database.BeginTransactionAsync(cancellationToken);

        return new EfCoreTransaction(transaction);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthDbContext).Assembly);
    }
}

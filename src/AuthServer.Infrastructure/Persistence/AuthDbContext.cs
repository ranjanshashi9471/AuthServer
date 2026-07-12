using AuthServer.Application.Abstractions.Persistence;
using AuthServer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Infrastructure.Persistence;

public sealed class AuthDbContext : DbContext, IUnitOfWork
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
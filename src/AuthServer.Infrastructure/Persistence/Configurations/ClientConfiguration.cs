using AuthServer.Domain.Entities;
using AuthServer.Domain.ValueObjects.Identifiers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthServer.Infrastructure.Persistence.Configurations;

internal sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("clients");

        builder.HasKey(c => c.Id);

        builder
            .Property(c => c.Id)
            .HasConversion(id => id.Value, value => new ClientId(value))
            .IsRequired();

        builder.Property(c => c.ClientIdentifier).HasMaxLength(50).IsRequired();

        builder.HasIndex(c => c.ClientIdentifier).IsUnique();

        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();

        builder.Property(c => c.Type).IsRequired();
        builder.Property(c => c.RequirePkce).IsRequired();
        builder.Property(c => c.Status).IsRequired();

        builder
            .HasMany(c => c.RedirectUris)
            .WithOne()
            .HasForeignKey(r => r.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(c => c.RedirectUris).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

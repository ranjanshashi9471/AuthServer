using AuthServer.Domain.Entities;
using AuthServer.Domain.ValueObjects.Identifiers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthServer.Infrastructure.Persistence.Configurations;

internal sealed class ClientRedirectUriConfiguration : IEntityTypeConfiguration<ClientRedirectUri>
{
    public void Configure(EntityTypeBuilder<ClientRedirectUri> builder)
    {
        builder.ToTable("client_redirect_uris");

        builder.HasKey(r => r.Id);

        builder
            .Property(r => r.Id)
            .HasConversion(id => id.Value, value => new ClientRedirectUriId(value))
            .IsRequired();

        builder.Property(r => r.Uri).HasMaxLength(400).IsRequired();

        // Enforce uniqueness per client
        builder.HasIndex(r => new { r.ClientId, r.Uri }).IsUnique();
    }
}

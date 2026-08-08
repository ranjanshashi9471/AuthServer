using AuthServer.Domain.Entities;
using AuthServer.Domain.ValueObjects.Identifiers;
using AuthServer.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthServer.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(rt => rt.Id);

        builder
            .Property(rt => rt.Id)
            .HasStronglyTypedIdConversion(value => RefreshTokenId.From(value));

        builder.Property(rt => rt.UserId).HasStronglyTypedIdConversion(value => UserId.From(value));

        builder.Property(rt => rt.TokenHash).HasMaxLength(512).IsRequired();

        builder.Property(rt => rt.CreatedAt).IsRequired();

        builder.Property(rt => rt.ExpiresAt).IsRequired();

        builder.Property(rt => rt.RevokedAt);

        builder
            .Property(rt => rt.ReplacedByTokenId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value == null ? null : RefreshTokenId.From(value.Value)
            );

        builder.HasIndex(rt => rt.TokenHash).IsUnique();

        builder.HasIndex(rt => rt.UserId);
    }
}

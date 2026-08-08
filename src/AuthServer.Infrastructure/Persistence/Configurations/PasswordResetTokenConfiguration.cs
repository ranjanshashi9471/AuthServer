using AuthServer.Domain.Entities;
using AuthServer.Domain.ValueObjects.Identifiers;
using AuthServer.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthServer.Infrastructure.Persistence.Configurations;

internal sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("password_reset_tokens");

        builder.HasKey(prt => prt.Id);

        builder
            .Property(prt => prt.Id)
            .HasStronglyTypedIdConversion(value => PasswordResetTokenId.From(value));

        builder
            .Property(prt => prt.UserId)
            .HasStronglyTypedIdConversion(value => UserId.From(value));

        builder.Property(prt => prt.TokenHash).HasMaxLength(512).IsRequired();

        builder.Property(prt => prt.CreatedAt).IsRequired();

        builder.Property(prt => prt.UpdatedAt).IsRequired();

        builder.Property(prt => prt.ExpiresAt).IsRequired();

        builder.Property(prt => prt.UsedAt);

        builder
            .HasOne(prt => prt.User)
            .WithMany()
            .HasForeignKey(prt => prt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(prt => prt.ExpiresAt);

        builder.HasIndex(prt => prt.UserId);
    }
}

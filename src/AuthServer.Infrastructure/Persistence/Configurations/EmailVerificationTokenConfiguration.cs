using AuthServer.Domain.Entities;
using AuthServer.Domain.ValueObjects.Identifiers;
using AuthServer.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthServer.Infrastructure.Persistence.Configurations;

internal sealed class EmailVerificationTokenConfiguration
    : IEntityTypeConfiguration<EmailVerificationToken>
{
    public void Configure(EntityTypeBuilder<EmailVerificationToken> builder)
    {
        builder.ToTable("email_verification_tokens");

        builder.HasKey(t => t.Id);

        builder
            .Property(t => t.Id)
            .HasStronglyTypedIdConversion(value => EmailVerificationTokenId.From(value));

        builder.Property(t => t.UserId).HasStronglyTypedIdConversion(value => UserId.From(value));

        builder.Property(t => t.TokenHash).HasMaxLength(512).IsRequired();

        // Explicit nullable timestamp mapping
        builder.Property(t => t.InvalidatedAt);

        builder.HasIndex(t => t.UserId);
        builder.HasIndex(t => t.ExpiresAt);

        builder
            .HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

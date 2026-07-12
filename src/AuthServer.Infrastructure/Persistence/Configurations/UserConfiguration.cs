using AuthServer.Domain.Entities;
using AuthServer.Domain.ValueObjects;
using AuthServer.Domain.ValueObjects.Identifiers;
using AuthServer.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthServer.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
               .HasStronglyTypedIdConversion(x => UserId.From(x));

        builder.Property(x => x.Email)
               .HasValueObjectConversion(
                    e => e.Value,
                    value => Email.Create(value))
               .HasMaxLength(255)
               .IsRequired();

        builder.HasIndex(x => x.Email)
               .IsUnique();

        builder.Property(x => x.Username)
               .HasValueObjectConversion(
                    u => u.Value,
                    value => Username.Create(value))
               .HasMaxLength(50)
               .IsRequired();

        builder.HasIndex(x => x.Username)
               .IsUnique();

        builder.Property(x => x.PasswordHash)
               .HasValueObjectConversion(
                    p => p.Value,
                    value => PasswordHash.From(value))
               .IsRequired();

        builder.Property(x => x.Status)
               .HasConversion<string>()
               .HasMaxLength(50)
               .IsRequired();

        builder.Property(x => x.CreatedAt)
               .IsRequired();

        builder.Property(x => x.UpdatedAt)
               .IsRequired();
    }
}
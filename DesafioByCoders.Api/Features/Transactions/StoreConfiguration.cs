using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DesafioByCoders.Api.Features.Transactions;

internal sealed class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.ToTable("stores");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
               .HasColumnName("id");

        builder.Property(x => x.Name)
               .HasColumnName("name")
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(x => x.Owner)
               .HasColumnName("owner")
               .HasMaxLength(100)
               .IsRequired();

        // Unique index to prevent duplicate store names
        builder.HasIndex(x => x.Name)
               .IsUnique()
               .HasDatabaseName("ix_stores_name_unique");

        // Non-unique index for case-insensitive search on store name
        builder.HasIndex(x => x.Name)
               .HasDatabaseName("ix_stores_name_search");
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DesafioByCoders.Api.Features;

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
               .HasConversion<int>()
               .IsRequired();

        builder.Property(x => x.Amount)
               .HasColumnType("numeric(18,2)")
               .IsRequired();

        builder.Property(x => x.SignedAmount)
               .HasColumnType("numeric(18,2)")
               .IsRequired();

        builder.Property(x => x.OccurredAtUtc)
               .HasColumnType("timestamp with time zone")
               .IsRequired();

        builder.Property(x => x.Cpf)
               .HasMaxLength(11)
               .IsRequired();

        builder.Property(x => x.Card)
               .HasMaxLength(12)
               .IsRequired();

        builder.Property(x => x.RawLineHash)
               .HasMaxLength(64)
               .IsRequired();

        builder.HasIndex(x => x.RawLineHash)
               .IsUnique();

        builder.Property(x => x.StoreId)
               .IsRequired();

        builder.HasOne<Store>()
               .WithMany()
               .HasForeignKey(x => x.StoreId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
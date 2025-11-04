﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DesafioByCoders.Api.Features.Transactions;

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
               .HasColumnName("id");

        builder.Property(x => x.Type)
               .HasColumnName("type")
               .HasConversion<int>()
               .IsRequired();

        builder.Property(x => x.Amount)
               .HasColumnName("amount")
               .HasColumnType("numeric(18,2)")
               .IsRequired();

        builder.Property(x => x.SignedAmount)
               .HasColumnName("signed_amount")
               .HasColumnType("numeric(18,2)")
               .IsRequired();

        builder.Property(x => x.OccurredAtUtc)
               .HasColumnName("occurred_at_utc")
               .HasColumnType("timestamp with time zone")
               .IsRequired();

        builder.Property(x => x.Cpf)
               .HasColumnName("cpf")
               .HasMaxLength(11)
               .IsRequired();

        builder.Property(x => x.Card)
               .HasColumnName("card")
               .HasMaxLength(12)
               .IsRequired();

        builder.Property(x => x.RawLineHash)
               .HasColumnName("raw_line_hash")
               .HasMaxLength(64)
               .IsRequired();

        builder.HasIndex(x => x.RawLineHash)
               .IsUnique();

        builder.Property(x => x.StoreId)
               .HasColumnName("store_id")
               .IsRequired();

        // Index on StoreId foreign key for efficient lookups and joins
        builder.HasIndex(x => x.StoreId)
               .HasDatabaseName("ix_transactions_store_id");

        builder.HasOne(x => x.Store)
               .WithMany()
               .HasForeignKey(x => x.StoreId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
using Microsoft.EntityFrameworkCore;
using DesafioByCoders.Api.Features;

namespace DesafioByCoders.Api.Features.Transactions;

internal sealed class TransactionDbContext : DbContext
{
    public TransactionDbContext(DbContextOptions<TransactionDbContext> options) : base(options) { }
    
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TransactionConfiguration());

        modelBuilder.ApplyConfiguration(new StoreConfiguration());
    }
}
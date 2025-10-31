using DesafioByCoders.Api.Features;
using DesafioByCoders.Api.Features.Transactions;
using DesafioByCoders.Api.Tests.Integrations;
using Microsoft.EntityFrameworkCore;

namespace DesafioByCoders.Api.Tests.Integrations.Features.Transactions;

public class TransactionRepositoryTests : IClassFixture<PostgresContainerFixture>
{
    private readonly PostgresContainerFixture fixture;

    public TransactionRepositoryTests(PostgresContainerFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task BulkInsertAsync_InsertsTransactionsAndPersistsFields()
    {
        await using var ctx = this.fixture.CreateDbContext();
        await ctx.Database.EnsureDeletedAsync();
        await ctx.Database.EnsureCreatedAsync();

        var store = Store.Create("Loja 1", "Owner");
        await ctx.Stores.AddAsync(store);
        await ctx.SaveChangesAsync();

        var t1 = Transaction.Create(store.Id, TransactionType.Debit, 100.00m, new DateTime(2024, 10, 5, 12, 0, 0, DateTimeKind.Utc), "12345678901", "111122223333", "raw-1");
        var t2 = Transaction.Create(store.Id, TransactionType.Boleto, 50.00m, new DateTime(2024, 10, 5, 13, 0, 0, DateTimeKind.Utc), "12345678901", "111122223333", "raw-2");
        var t3 = Transaction.Create(store.Id, TransactionType.Credit, 0.01m, new DateTime(2024, 10, 5, 14, 0, 0, DateTimeKind.Utc), "12345678901", "111122223333", "raw-3");

        var repo = new TransactionRepository(ctx);

        await repo.BulkInsertAsync(new[] { t1, t2, t3 }, CancellationToken.None);

        var transactions = await ctx.Transactions.AsNoTracking()
            .OrderBy(t => t.OccurredAtUtc)
            .ToListAsync();

        Assert.Equal(3, transactions.Count);

        Assert.Equal(TransactionType.Debit, transactions[0].Type);
        Assert.Equal(100.00m, transactions[0].Amount);
        Assert.Equal(100.00m, transactions[0].SignedAmount);

        Assert.Equal(TransactionType.Boleto, transactions[1].Type);
        Assert.Equal(50.00m, transactions[1].Amount);
        Assert.Equal(-50.00m, transactions[1].SignedAmount);

        Assert.Equal(TransactionType.Credit, transactions[2].Type);
        Assert.Equal(0.01m, transactions[2].Amount);
        Assert.Equal(0.01m, transactions[2].SignedAmount);
    }

    [Fact]
    public async Task BulkInsertAsync_WithDuplicateRawLineHash_ShouldThrow()
    {
        await using var ctx = this.fixture.CreateDbContext();
        await ctx.Database.EnsureDeletedAsync();
        await ctx.Database.EnsureCreatedAsync();

        var store = Store.Create("Loja 2", "Owner");
        await ctx.Stores.AddAsync(store);
        await ctx.SaveChangesAsync();

        // same raw line to force duplicate RawLineHash unique constraint violation
        var raw = "DUPLICATE-RAW";
        var a = Transaction.Create(store.Id, TransactionType.Credit, 10.00m, new DateTime(2024, 10, 6, 12, 0, 0, DateTimeKind.Utc), "12345678901", "111122223333", raw);
        var b = Transaction.Create(store.Id, TransactionType.Debit, 5.00m, new DateTime(2024, 10, 6, 13, 0, 0, DateTimeKind.Utc), "12345678901", "111122223333", raw);

        var repo = new TransactionRepository(ctx);

        var ex = await Record.ExceptionAsync(() => repo.BulkInsertAsync(new[] { a, b }, CancellationToken.None));

        Assert.NotNull(ex);
    }
}
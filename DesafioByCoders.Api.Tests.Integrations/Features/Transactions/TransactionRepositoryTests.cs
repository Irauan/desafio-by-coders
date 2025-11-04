using DesafioByCoders.Api.Features;
using DesafioByCoders.Api.Features.Transactions;
using Microsoft.EntityFrameworkCore;

namespace DesafioByCoders.Api.Tests.Integrations.Features.Transactions;

public class TransactionRepositoryTests : IClassFixture<PostgresContainerFixture>,
                                          IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;

    public TransactionRepositoryTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await using var ctx = _fixture.CreateDbContext<TransactionDbContext>();

        await ctx.Database.EnsureDeletedAsync();
        await ctx.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task BulkInsertAsync_InsertsTransactionsAndPersistsFields()
    {
        await using var ctx = _fixture.CreateDbContext<TransactionDbContext>();

        var store = Store.Create("Loja 1", "Owner");
        await ctx.Stores.AddAsync(store);
        await ctx.SaveChangesAsync();

        var t1 = Transaction.Create(
            store.Id,
            TransactionType.Debit,
            100.00m,
            new DateTime(
                2024,
                10,
                5,
                12,
                0,
                0,
                DateTimeKind.Utc
            ),
            "12345678901",
            "111122223333",
            "raw-1"
        );
        var t2 = Transaction.Create(
            store.Id,
            TransactionType.Boleto,
            50.00m,
            new DateTime(
                2024,
                10,
                5,
                13,
                0,
                0,
                DateTimeKind.Utc
            ),
            "12345678901",
            "111122223333",
            "raw-2"
        );
        var t3 = Transaction.Create(
            store.Id,
            TransactionType.Credit,
            0.01m,
            new DateTime(
                2024,
                10,
                5,
                14,
                0,
                0,
                DateTimeKind.Utc
            ),
            "12345678901",
            "111122223333",
            "raw-3"
        );

        var repo = new TransactionRepository(ctx);

        await repo.BulkInsertAsync([t1, t2, t3], CancellationToken.None);

        var transactions = await ctx.Transactions
                                    .AsNoTracking()
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
        await using var ctx = _fixture.CreateDbContext<TransactionDbContext>();

        var store = Store.Create("Loja 2", "Owner");
        await ctx.Stores.AddAsync(store);
        await ctx.SaveChangesAsync();

        // same raw line to force duplicate RawLineHash unique constraint violation
        var raw = "DUPLICATE-RAW";
        var a = Transaction.Create(
            store.Id,
            TransactionType.Credit,
            10.00m,
            new DateTime(
                2024,
                10,
                6,
                12,
                0,
                0,
                DateTimeKind.Utc
            ),
            "12345678901",
            "111122223333",
            raw
        );
        var b = Transaction.Create(
            store.Id,
            TransactionType.Debit,
            5.00m,
            new DateTime(
                2024,
                10,
                6,
                13,
                0,
                0,
                DateTimeKind.Utc
            ),
            "12345678901",
            "111122223333",
            raw
        );

        var repo = new TransactionRepository(ctx);

        var ex = await Record.ExceptionAsync(() => repo.BulkInsertAsync([a, b], CancellationToken.None));

        Assert.NotNull(ex);
    }

    [Fact]
    public async Task GetExistingHashesAsync_WithEmptyInput_ReturnsEmptySet()
    {
        // Arrange
        await using var ctx = _fixture.CreateDbContext<TransactionDbContext>();
        var repo = new TransactionRepository(ctx);
        var emptyHashes = Array.Empty<string>();

        // Act
        var result = await repo.GetExistingHashesAsync(emptyHashes, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetExistingHashesAsync_WithNoMatchingHashes_ReturnsEmptySet()
    {
        // Arrange
        await using var ctx = _fixture.CreateDbContext<TransactionDbContext>();

        var store = Store.Create("Loja Test", "Owner Test");
        await ctx.Stores.AddAsync(store);
        await ctx.SaveChangesAsync();

        var transaction = Transaction.Create(
            store.Id,
            TransactionType.Credit,
            100.00m,
            new DateTime(2024, 10, 10, 12, 0, 0, DateTimeKind.Utc),
            "12345678901",
            "111122223333",
            "existing-raw-line"
        );

        await ctx.Transactions.AddAsync(transaction);
        await ctx.SaveChangesAsync();

        var repo = new TransactionRepository(ctx);
        var nonExistentHashes = new[] { "hash-does-not-exist-1", "hash-does-not-exist-2" };

        // Act
        var result = await repo.GetExistingHashesAsync(nonExistentHashes, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetExistingHashesAsync_WithAllMatchingHashes_ReturnsAllHashes()
    {
        // Arrange
        await using var ctx = _fixture.CreateDbContext<TransactionDbContext>();

        var store = Store.Create("Loja All Match", "Owner");
        await ctx.Stores.AddAsync(store);
        await ctx.SaveChangesAsync();

        var t1 = Transaction.Create(
            store.Id,
            TransactionType.Credit,
            100.00m,
            new DateTime(2024, 10, 11, 12, 0, 0, DateTimeKind.Utc),
            "12345678901",
            "111122223333",
            "raw-line-1"
        );
        var t2 = Transaction.Create(
            store.Id,
            TransactionType.Debit,
            50.00m,
            new DateTime(2024, 10, 11, 13, 0, 0, DateTimeKind.Utc),
            "12345678901",
            "111122223333",
            "raw-line-2"
        );
        var t3 = Transaction.Create(
            store.Id,
            TransactionType.Boleto,
            25.00m,
            new DateTime(2024, 10, 11, 14, 0, 0, DateTimeKind.Utc),
            "12345678901",
            "111122223333",
            "raw-line-3"
        );

        await ctx.Transactions.AddRangeAsync(t1, t2, t3);
        await ctx.SaveChangesAsync();

        var repo = new TransactionRepository(ctx);
        var searchHashes = new[] { t1.RawLineHash, t2.RawLineHash, t3.RawLineHash };

        // Act
        var result = await repo.GetExistingHashesAsync(searchHashes, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Contains(t1.RawLineHash, result);
        Assert.Contains(t2.RawLineHash, result);
        Assert.Contains(t3.RawLineHash, result);
    }

    [Fact]
    public async Task GetExistingHashesAsync_WithPartialMatches_ReturnsOnlyMatchingHashes()
    {
        // Arrange
        await using var ctx = _fixture.CreateDbContext<TransactionDbContext>();

        var store = Store.Create("Loja Partial", "Owner");
        await ctx.Stores.AddAsync(store);
        await ctx.SaveChangesAsync();

        var t1 = Transaction.Create(
            store.Id,
            TransactionType.Credit,
            100.00m,
            new DateTime(2024, 10, 12, 12, 0, 0, DateTimeKind.Utc),
            "12345678901",
            "111122223333",
            "existing-raw-1"
        );
        var t2 = Transaction.Create(
            store.Id,
            TransactionType.Debit,
            50.00m,
            new DateTime(2024, 10, 12, 13, 0, 0, DateTimeKind.Utc),
            "12345678901",
            "111122223333",
            "existing-raw-2"
        );

        await ctx.Transactions.AddRangeAsync(t1, t2);
        await ctx.SaveChangesAsync();

        var repo = new TransactionRepository(ctx);
        // Mix of existing and non-existing hashes
        var searchHashes = new[] 
        { 
            t1.RawLineHash,              // exists
            "non-existent-hash-1",       // doesn't exist
            t2.RawLineHash,              // exists
            "non-existent-hash-2"        // doesn't exist
        };

        // Act
        var result = await repo.GetExistingHashesAsync(searchHashes, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(t1.RawLineHash, result);
        Assert.Contains(t2.RawLineHash, result);
        Assert.DoesNotContain("non-existent-hash-1", result);
        Assert.DoesNotContain("non-existent-hash-2", result);
    }

    [Fact]
    public async Task GetExistingHashesAsync_WithDuplicateHashesInInput_ReturnsUniqueHashes()
    {
        // Arrange
        await using var ctx = _fixture.CreateDbContext<TransactionDbContext>();

        var store = Store.Create("Loja Duplicate Input", "Owner");
        await ctx.Stores.AddAsync(store);
        await ctx.SaveChangesAsync();

        var transaction = Transaction.Create(
            store.Id,
            TransactionType.Credit,
            100.00m,
            new DateTime(2024, 10, 13, 12, 0, 0, DateTimeKind.Utc),
            "12345678901",
            "111122223333",
            "duplicate-raw"
        );

        await ctx.Transactions.AddAsync(transaction);
        await ctx.SaveChangesAsync();

        var repo = new TransactionRepository(ctx);
        // Input contains the same hash multiple times
        var searchHashes = new[] 
        { 
            transaction.RawLineHash,
            transaction.RawLineHash,
            transaction.RawLineHash
        };

        // Act
        var result = await repo.GetExistingHashesAsync(searchHashes, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Contains(transaction.RawLineHash, result);
    }

    [Fact]
    public async Task GetExistingHashesAsync_WithLargeDataset_PerformsEfficiently()
    {
        // Arrange
        await using var ctx = _fixture.CreateDbContext<TransactionDbContext>();

        var store = Store.Create("Loja Large", "Owner");
        await ctx.Stores.AddAsync(store);
        await ctx.SaveChangesAsync();

        // Insert 100 transactions
        var transactions = new List<Transaction>();
        for (var i = 0; i < 100; i++)
        {
            var transaction = Transaction.Create(
                store.Id,
                TransactionType.Credit,
                i * 10.00m,
                new DateTime(2024, 10, 14, 12, 0, 0, DateTimeKind.Utc).AddMinutes(i),
                "12345678901",
                "111122223333",
                $"large-dataset-raw-{i}"
            );
            transactions.Add(transaction);
        }

        await ctx.Transactions.AddRangeAsync(transactions);
        await ctx.SaveChangesAsync();

        var repo = new TransactionRepository(ctx);
        
        // Search for 50 existing and 50 non-existing hashes
        var searchHashes = new List<string>();
        for (var i = 0; i < 50; i++)
        {
            searchHashes.Add(transactions[i].RawLineHash); // existing
        }
        for (var i = 0; i < 50; i++)
        {
            searchHashes.Add($"non-existent-hash-{i}"); // non-existing
        }

        // Act
        var result = await repo.GetExistingHashesAsync(searchHashes, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(50, result.Count);
        
        // Verify all existing hashes are returned
        for (var i = 0; i < 50; i++)
        {
            Assert.Contains(transactions[i].RawLineHash, result);
        }
        
        // Verify non-existing hashes are not returned
        for (var i = 0; i < 50; i++)
        {
            Assert.DoesNotContain($"non-existent-hash-{i}", result);
        }
    }

    [Fact]
    public async Task GetExistingHashesAsync_WithCancellationToken_RespectsCancel()
    {
        // Arrange
        await using var ctx = _fixture.CreateDbContext<TransactionDbContext>();

        var store = Store.Create("Loja Cancel", "Owner");
        await ctx.Stores.AddAsync(store);
        await ctx.SaveChangesAsync();

        var transaction = Transaction.Create(
            store.Id,
            TransactionType.Credit,
            100.00m,
            new DateTime(2024, 10, 15, 12, 0, 0, DateTimeKind.Utc),
            "12345678901",
            "111122223333",
            "cancel-test-raw"
        );

        await ctx.Transactions.AddAsync(transaction);
        await ctx.SaveChangesAsync();

        var repo = new TransactionRepository(ctx);
        var searchHashes = new[] { transaction.RawLineHash };
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await repo.GetExistingHashesAsync(searchHashes, cts.Token)
        );
    }
}
using DesafioByCoders.Api.Features;
using DesafioByCoders.Api.Features.Transactions;
using Microsoft.EntityFrameworkCore;

namespace DesafioByCoders.Api.Tests.Integrations.Features.Transactions;

public class StoreRepositoryTests : IClassFixture<PostgresContainerFixture>,
                                    IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;

    public StoreRepositoryTests(PostgresContainerFixture fixture)
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
    public async Task GetExistentStores_ReturnsOnlyMatchingByLowercaseName()
    {
        await using var ctx = _fixture.CreateDbContext<TransactionDbContext>();

        var s1 = Store.Create("MERCADO A", "ALICE");
        var s2 = Store.Create("Farmacia B", "BOB");
        var s3 = Store.Create("Padaria C", "CAROL");

        await ctx.Stores.AddRangeAsync(s1, s2, s3);
        await ctx.SaveChangesAsync();

        var repo = new StoreRepository(ctx);

        var identifiers = new HashSet<string>
        {
            "mercado a",
            "farmacia b"
        };

        var result = await repo.GetExistentStores(identifiers);

        Assert.Equal(2, result.Count);
        Assert.Contains("mercado a", result.Keys);
        Assert.Contains("farmacia b", result.Keys);
        Assert.DoesNotContain("padaria c", result.Keys);
    }

    [Fact]
    public async Task BulkInsertAsync_InsertsStoresAndSetsIdentity()
    {
        await using var ctx = _fixture.CreateDbContext<TransactionDbContext>();

        var toInsert = new List<Store>
        {
            Store.Create("Loja X", "Xavier"),
            Store.Create("Loja Y", "Yara")
        };

        var repo = new StoreRepository(ctx);

        await repo.BulkInsertAsync(toInsert, CancellationToken.None);

        Assert.All(toInsert, s => Assert.True(s.Id > 0));

        var all = await ctx.Stores.AsNoTracking()
                           .OrderBy(s => s.Id)
                           .ToListAsync();

        Assert.Equal(2, all.Count);
        Assert.Equal(
            "loja x",
            all[0]
                .ToString()
        );
        Assert.Equal(
            "loja y",
            all[1]
                .ToString()
        );
    }
}
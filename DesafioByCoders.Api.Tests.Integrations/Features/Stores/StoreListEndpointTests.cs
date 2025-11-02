using System.Net;
using System.Text.Json;
using DesafioByCoders.Api.Features.Transactions;
using DesafioByCoders.Api.Tests.Integrations.Infrastructure;

namespace DesafioByCoders.Api.Tests.Integrations.Features.Stores;

public class StoreListEndpointTests : IClassFixture<ApiFixture>, IAsyncLifetime
{
    private readonly ApiFixture _fixture;

    public StoreListEndpointTests(ApiFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await using var dbContext = _fixture.CreateDbContext<TransactionDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Get_WhenNoStores_ReturnsEmptyList()
    {
        var items = await GetStoresAsync();

        Assert.Empty(items);
    }

    [Fact]
    public async Task Get_WhenStoresWithoutTransactions_ReturnsZeroBalance()
    {
        await SeedStoresAndReturnAsync(("Market A", "Alice"), ("Bakery B", "Bob"));

        var items = await GetStoresAsync();

        Assert.Equal(2, items.Count);
        Assert.All(items, x => Assert.Equal(0m, x.Balance));
    }

    [Fact]
    public async Task Get_WhenStoresWithTransactions_ReturnsCorrectBalancesSortedByName()
    {
        var stores = await SeedStoresAndReturnAsync(("Alpha Store", "Ana"), ("Zeta Store", "Zoe"));

        var alpha = stores.First(s => s.Name == "Alpha Store");
        var zeta = stores.First(s => s.Name == "Zeta Store");

        await SeedTransactionsAsync(
            MakeTransaction(alpha.Id, TransactionType.Credit, 100m, "raw1"),
            MakeTransaction(alpha.Id, TransactionType.Boleto, 40m, "raw2"),
            MakeTransaction(zeta.Id, TransactionType.Sales, 50m, "raw3"),
            MakeTransaction(zeta.Id, TransactionType.Rent, 10m, "raw4")
        );

        var items = await GetStoresAsync();

        // Should be sorted by name ascending: Alpha Store, Zeta Store
        Assert.Equal(2, items.Count);
        Assert.Equal("Alpha Store", items[0].Name);
        Assert.Equal("Zeta Store", items[1].Name);

        // Balances: Alpha = +100 - 40 = 60; Zeta = +50 - 10 = 40
        Assert.Equal(60m, items[0].Balance);
        Assert.Equal(40m, items[1].Balance);
    }

    private async Task<List<StoreListItemDto>> GetStoresAsync()
    {
        var response = await _fixture.Client.GetAsync("/api/v1/stores");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadAsStringAsync();

        var items = JsonSerializer.Deserialize<List<StoreListItemDto>>(payload, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new List<StoreListItemDto>();

        return items;
    }

    private async Task<List<Store>> SeedStoresAndReturnAsync(params (string Name, string Owner)[] stores)
    {
        await using var db = _fixture.CreateDbContext<TransactionDbContext>();

        var entities = new List<Store>();

        foreach (var s in stores)
        {
            var entity = Store.Create(s.Name, s.Owner);

            entities.Add(entity);

            await db.Stores.AddAsync(entity);
        }

        await db.SaveChangesAsync();

        return entities;
    }

    private async Task SeedTransactionsAsync(params Transaction[] transactions)
    {
        await using var db = _fixture.CreateDbContext<TransactionDbContext>();

        await db.Transactions.AddRangeAsync(transactions);

        await db.SaveChangesAsync();
    }

    private static Transaction MakeTransaction(int storeId, TransactionType type, decimal amount, string raw)
    {
        var now = DateTime.UtcNow;
        var cpf = "12345678901";
        var card = "123456789012";

        var tx = Transaction.Create(storeId, type, amount, now, cpf, card, raw);

        return tx;
    }

    private sealed class StoreListItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;
        public decimal Balance { get; set; }
    }
}
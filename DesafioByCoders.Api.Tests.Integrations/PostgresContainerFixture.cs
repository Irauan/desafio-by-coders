using DesafioByCoders.Api.Features.Transactions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace DesafioByCoders.Api.Tests.Integrations;

public sealed class PostgresContainerFixture : IAsyncLifetime,
                                               IDisposable
{
    private readonly PostgreSqlContainer _container;

    public string ConnectionString { get; private set; } = string.Empty;

    public PostgresContainerFixture()
    {
        _container = new PostgreSqlBuilder().WithDatabase("testdb")
                                            .WithUsername("postgres")
                                            .WithPassword("postgres")
                                            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        ConnectionString = _container.GetConnectionString();

        var options = new DbContextOptionsBuilder<TransactionDbContext>()
                      .UseNpgsql(ConnectionString)
                      .Options;

        await using var ctx = new TransactionDbContext(options);
        await ctx.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public void Dispose()
    {
        _container.DisposeAsync()
                  .AsTask()
                  .GetAwaiter()
                  .GetResult();
    }

    internal TransactionDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TransactionDbContext>()
                      .UseNpgsql(ConnectionString)
                      .Options;

        return new TransactionDbContext(options);
    }
}
using DesafioByCoders.Api.Features.Transactions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace DesafioByCoders.Api.Tests.Integrations;

public class PostgresContainerFixture : IAsyncLifetime,
                                        IDisposable
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder().WithDatabase("testdb")
                                                                             .WithUsername("postgres")
                                                                             .WithPassword("postgres")
                                                                             .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        ConnectionString = _container.GetConnectionString();
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

    public TDbContext CreateDbContext<TDbContext>() where TDbContext : DbContext
    {
        var options = new DbContextOptionsBuilder<TDbContext>().UseNpgsql(ConnectionString)
                                                               .Options;

        var instance = Activator.CreateInstance(typeof(TDbContext), options);

        if (instance is null)
        {
            throw new InvalidOperationException($"Could not create DbContext of type {typeof(TDbContext).FullName}. Ensure it has a constructor that accepts DbContextOptions<{typeof(TDbContext).Name}>.");
        }

        return (TDbContext)instance;
    }
}
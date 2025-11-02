using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DesafioByCoders.Api.Tests.Integrations.Infrastructure;

using Api;

public sealed class ApiFixture : PostgresContainerFixture, IAsyncLifetime
{
    private WebApplicationFactory<Program>? _factory;

    public HttpClient Client { get; private set; } = null!;

    public new async Task InitializeAsync()
    {
        await base.InitializeAsync();

        Environment.SetEnvironmentVariable("ConnectionStrings__desafiobycoders", ConnectionString);
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((ctx, config) =>
                {
                    var dict = new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:desafiobycoders"] = ConnectionString,
                        ["ASPNETCORE_ENVIRONMENT"] = "Test"
                    };

                    config.AddInMemoryCollection(dict!);
                });
            });

        Client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public IServiceScope CreateScope()
    {
        return _factory!.Services.CreateScope();
    }

    public new async Task DisposeAsync()
    {
        _factory?.Dispose();
        await base.DisposeAsync();
    }
}
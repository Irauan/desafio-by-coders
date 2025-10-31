using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace DesafioByCoders.Api;

internal sealed class ApplyMigrations : IHostedService
{
    private readonly IServiceProvider serviceProvider;

    private readonly ILogger<ApplyMigrations> logger;

    public ApplyMigrations(IServiceProvider serviceProvider, ILogger<ApplyMigrations> logger)
    {
        this.serviceProvider = serviceProvider;

        this.logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var assembly = typeof(Program).Assembly;

        var dbContextTypes = assembly
                             .GetTypes()
                             .Where(type => type is
                                            {
                                                IsAbstract: false,
                                                IsGenericTypeDefinition: false
                                            } &&
                                            typeof(DbContext).IsAssignableFrom(type)
                             )
                             .ToArray();

        if (dbContextTypes.Length == 0)
        {
            logger.LogInformation("No DbContext types found for migration in assembly {Assembly}", assembly.FullName);

            return;
        }

        using var scope = serviceProvider.CreateScope();

        foreach (var contextType in dbContextTypes)
        {
            try
            {
                var context = scope.ServiceProvider.GetService(contextType) as DbContext;

                if (context is null)
                {
                    logger.LogDebug("DbContext type {ContextType} is not registered in DI. Skipping.", contextType.FullName);

                    continue;
                }

                logger.LogInformation("Applying migrations for {ContextType}", contextType.Name);

                await context.Database.MigrateAsync(cancellationToken);

                if (context is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync();
                }
                else
                {
                    context.Dispose();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to apply migrations for {ContextType}", contextType.FullName);

                throw;
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
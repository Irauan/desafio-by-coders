using Microsoft.EntityFrameworkCore;

namespace DesafioByCoders.Api.Features.Transactions;

internal static class TransactionSliceBuilder
{
    internal static IServiceCollection AddTransactionSlice(this IServiceCollection services, string connectionString)
    {
        services.AddScoped<IStoreRepository, StoreRepository>()
                .AddDbContext<TransactionDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }
}
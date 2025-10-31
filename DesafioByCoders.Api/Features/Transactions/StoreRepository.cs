using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;

namespace DesafioByCoders.Api.Features.Transactions;

internal class StoreRepository : IStoreRepository
{
    private readonly TransactionDbContext _dbContext;

    public StoreRepository(TransactionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Dictionary<string, Store>> GetExistentStores(HashSet<string> storeIdentifiers)
    {
        return await _dbContext.Stores
                               .AsNoTracking()
                               .Where(store => storeIdentifiers.Contains(store.Name.ToLower()))
                               .ToDictionaryAsync(store => store.ToString(), store => store);
    }

    public async Task BulkInsertAsync(IEnumerable<Store> stores, CancellationToken cancellationToken = default)
    {
        await _dbContext.BulkInsertAsync(
            stores,
            new BulkConfig()
            {
                SetOutputIdentity = true,
                PreserveInsertOrder = false,
                BatchSize = 1000
            },
            cancellationToken: cancellationToken
        );
    }
}
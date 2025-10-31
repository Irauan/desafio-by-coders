namespace DesafioByCoders.Api.Features.Transactions;

internal interface IStoreRepository
{
    Task<Dictionary<string, Store>> GetExistentStores(HashSet<string> storeIdentifiers);
    Task BulkInsertAsync(IEnumerable<Store> stores, CancellationToken cancellationToken = default);
}
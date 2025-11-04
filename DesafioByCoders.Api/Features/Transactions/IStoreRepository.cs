namespace DesafioByCoders.Api.Features.Transactions;

/// <summary>
/// Defines the contract for store data access operations.
/// </summary>
/// <remarks>
/// This repository provides methods for querying existing stores and persisting new stores.
/// Store identifiers are case-insensitive and are normalized to lowercase.
/// </remarks>
internal interface IStoreRepository
{
    /// <summary>
    /// Retrieves existing stores from the database that match the provided identifiers.
    /// </summary>
    /// <param name="storeIdentifiers">
    /// A hash set of store identifiers to search for.
    /// Store identifiers are in the format "storename" (lowercase, normalized).
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains a dictionary where:
    /// <list type="bullet">
    ///   <item><description>Key: Store identifier (lowercase normalized string)</description></item>
    ///   <item><description>Value: The corresponding <see cref="Store"/> entity from the database</description></item>
    /// </list>
    /// If no stores are found matching the identifiers, an empty dictionary is returned.
    /// </returns>
    /// <remarks>
    /// This method is used during transaction import to identify which stores already exist
    /// in the database, avoiding duplicate store creation. The store identifier is derived
    /// from the store name and is case-insensitive.
    /// </remarks>
    Task<Dictionary<string, Store>> GetExistentStores(HashSet<string> storeIdentifiers);
    
    /// <summary>
    /// Inserts multiple stores into the database in a single bulk operation.
    /// </summary>
    /// <param name="stores">The collection of stores to insert.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. Default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    /// This method uses bulk insert operations for optimal performance when inserting multiple stores.
    /// After insertion, the store IDs are automatically populated and can be used for transaction relationships.
    /// Stores with duplicate identifiers (case-insensitive store names) will violate the unique constraint.
    /// </remarks>
    /// <exception cref="Microsoft.EntityFrameworkCore.DbUpdateException">
    /// Thrown when a database constraint violation occurs, such as duplicate store identifiers.
    /// </exception>
    Task BulkInsertAsync(IEnumerable<Store> stores, CancellationToken cancellationToken = default);
}
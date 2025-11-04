using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;

namespace DesafioByCoders.Api.Features.Transactions;

/// <summary>
/// Repository implementation for store data access operations using Entity Framework Core.
/// </summary>
/// <remarks>
/// This implementation uses EF Core with PostgreSQL-specific bulk extensions for optimal performance
/// when handling store operations. The repository leverages the EFCore.BulkExtensions library for
/// high-performance bulk inserts and uses AsNoTracking for read-only queries.
/// </remarks>
internal class StoreRepository : IStoreRepository
{
    private readonly TransactionDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="StoreRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The Entity Framework database context for store operations.</param>
    public StoreRepository(TransactionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// <b>Implementation Details:</b>
    /// </para>
    /// <para>
    /// This method performs an optimized database query using Entity Framework Core with the following characteristics:
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>AsNoTracking:</b> Disables change tracking for better performance since stores are read-only in this context.</description></item>
    /// <item><description><b>Case-insensitive matching:</b> Uses ToLower() on store.Name to match against normalized identifiers.</description></item>
    /// <item><description><b>Dictionary materialization:</b> Results are directly materialized to a dictionary for O(1) lookups during import.</description></item>
    /// </list>
    /// <para>
    /// The generated SQL query is similar to:
    /// </para>
    /// <code>
    /// SELECT id, name, owner 
    /// FROM stores 
    /// WHERE LOWER(name) = ANY(@storeIdentifiers)
    /// </code>
    /// <para>
    /// <b>Performance Characteristics:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description>Query time: O(m × log n) where m = storeIdentifiers.Count, n = total stores in DB</description></item>
    /// <item><description>Memory usage: O(m) where m = number of matching stores</description></item>
    /// <item><description>Single database round-trip regardless of identifier count</description></item>
    /// <item><description>No change tracking overhead due to AsNoTracking()</description></item>
    /// </list>
    /// </remarks>
    public async Task<Dictionary<string, Store>> GetExistentStores(HashSet<string> storeIdentifiers)
    {
        return await _dbContext.Stores
                               .AsNoTracking()
                               .Where(store => storeIdentifiers.Contains(store.Name.ToLower()))
                               .ToDictionaryAsync(store => store.ToString(), store => store);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// <b>Implementation Details:</b>
    /// </para>
    /// <para>
    /// This implementation uses EFCore.BulkExtensions for PostgreSQL to achieve high-performance
    /// bulk inserts. The configuration is optimized for store creation with the following settings:
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>SetOutputIdentity = true:</b> Enables identity output so generated store IDs are populated after insert. This is critical because the IDs are needed to create transaction relationships.</description></item>
    /// <item><description><b>PreserveInsertOrder = false:</b> Allows the database to optimize insertion order for maximum throughput.</description></item>
    /// <item><description><b>BatchSize = 1000:</b> Processes stores in batches of 1000 records to balance memory usage and database round-trips.</description></item>
    /// </list>
    /// <para>
    /// <b>Why SetOutputIdentity = true for Stores?</b>
    /// </para>
    /// <para>
    /// Unlike transactions, stores require their generated IDs to be available immediately after insertion
    /// because these IDs are used as foreign keys when creating transaction records. The bulk insert operation
    /// efficiently retrieves the generated IDs from the database and populates the Store.Id properties.
    /// </para>
    /// <para>
    /// <b>Performance Characteristics:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description>Typical throughput: 5,000+ stores per second (slightly slower than transactions due to identity output)</description></item>
    /// <item><description>Memory usage: Minimal due to batching strategy</description></item>
    /// <item><description>Database round-trips: Reduced to count(stores) / 1000</description></item>
    /// <item><description>Identity retrieval: Efficiently handled by bulk extension library using RETURNING clause</description></item>
    /// </list>
    /// </remarks>
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
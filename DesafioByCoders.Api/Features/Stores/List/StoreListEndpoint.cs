using System.Data;
using Asp.Versioning;
using Dapper;

namespace DesafioByCoders.Api.Features.Stores.List;

/// <summary>
/// Delivers a clear, up-to-date list of every store and its net balance to support daily reconciliation and decisions.
/// </summary>
internal static class StoreListEndpoint
{
    /// <summary>
    /// Publishes the "Store balances" list used by dashboards and back-office to see each store's net position at a glance.
    /// </summary>
    internal static void MapStoreList(this IEndpointRouteBuilder endpoints)
    {
        var apiVersionSet = endpoints.NewApiVersionSet()
                                     .HasApiVersion(new ApiVersion(1, 0))
                                     .ReportApiVersions()
                                     .Build();

        var group = endpoints.MapGroup("api/v{version:apiVersion}/stores")
                             .WithApiVersionSet(apiVersionSet)
                             .WithTags("Store");

        group.MapGet("", HandleList)
             .WithName("store-list")
             .Produces<List<StoreListItem>>()
             .Produces(StatusCodes.Status500InternalServerError)
             .WithDescription("Returns all stores with their current net balance (credits minus debits).");
    }

    /// <summary>
    /// Handles GET requests to retrieve all stores with their calculated net balances.
    /// </summary>
    /// <param name="dbConnection">Database connection injected from DI container for executing queries.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests during the async operation.</param>
    /// <returns>
    /// An <see cref="IResult"/> containing:
    /// <list type="bullet">
    /// <item><description><b>200 OK</b> with a list of <see cref="StoreListItem"/> objects representing all stores and their balances.</description></item>
    /// <item><description><b>500 Internal Server Error</b> if a database or unexpected error occurs.</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// <para>
    /// This endpoint retrieves all stores from the database along with their calculated net balance.
    /// The balance is computed as the sum of all signed transaction amounts (credits minus debits) for each store.
    /// </para>
    /// <para>
    /// <b>Query Details:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description>Uses LEFT JOIN to include stores even if they have no transactions (balance will be 0)</description></item>
    /// <item><description>COALESCE ensures null sums are converted to 0 for stores without transactions</description></item>
    /// <item><description>Groups by store.id, store.name, and store.owner to aggregate transaction amounts</description></item>
    /// <item><description>Orders results alphabetically by store name for consistent presentation</description></item>
    /// </list>
    /// </remarks>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled via the <paramref name="cancellationToken"/>.</exception>
    private static async Task<IResult> HandleList(IDbConnection dbConnection, CancellationToken cancellationToken)
    {
        if (dbConnection.State != ConnectionState.Open)
        {
            dbConnection.Open();
        }

        var sql = """
                    select s.id as id, s.name as name, s.owner as owner, coalesce(sum(t.signed_amount), 0) as balance
                    from stores s
                    left join transactions t on t.store_id = s.id
                    group by s.id, s.name, s.owner
                    order by s.name;
                  """;

        var stores = await dbConnection.QueryAsync<StoreListItem>(new CommandDefinition(sql, cancellationToken: cancellationToken));

        return Results.Ok(stores);
    }

    internal sealed record StoreListItem(
        int Id,
        string Name,
        string Owner,
        decimal Balance
    );
}
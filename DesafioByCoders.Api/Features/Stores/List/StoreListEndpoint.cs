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
    /// Returns all stores with their current net balance (credits minus debits). No filters applied.
    /// </summary>
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
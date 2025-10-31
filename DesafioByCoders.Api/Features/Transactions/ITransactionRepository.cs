namespace DesafioByCoders.Api.Features.Transactions;

internal interface ITransactionRepository
{
    Task BulkInsertAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken = default);
}
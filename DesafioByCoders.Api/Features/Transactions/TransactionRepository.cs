using EFCore.BulkExtensions;

namespace DesafioByCoders.Api.Features.Transactions;

internal class TransactionRepository : ITransactionRepository
{
    private readonly TransactionDbContext _dbContext;

    public TransactionRepository(TransactionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task BulkInsertAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken = default)
    {
        await _dbContext.BulkInsertAsync(
            transactions,
            config =>
            {
                config.SetOutputIdentity = false;
                config.PreserveInsertOrder = false;
                config.BatchSize = 1000;
            },
            cancellationToken: cancellationToken
        );
    }
}
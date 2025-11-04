namespace DesafioByCoders.Api.Features.Transactions;

/// <summary>
/// Defines the contract for transaction data access operations.
/// </summary>
/// <remarks>
/// This repository provides methods for persisting transactions and checking for duplicates.
/// All operations are designed to work efficiently with large datasets using bulk operations.
/// </remarks>
internal interface ITransactionRepository
{
    /// <summary>
    /// Inserts multiple transactions into the database in a single bulk operation.
    /// </summary>
    /// <param name="transactions">The collection of transactions to insert.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. Default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    /// This method uses bulk insert operations for optimal performance when inserting large numbers of transactions.
    /// Transactions with duplicate <see cref="Transaction.RawLineHash"/> values will violate the unique constraint.
    /// </remarks>
    /// <exception cref="Microsoft.EntityFrameworkCore.DbUpdateException">
    /// Thrown when a database constraint violation occurs, such as duplicate raw line hashes.
    /// </exception>
    Task BulkInsertAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Returns a hash set of transaction hashes that already exist in the database.
    /// Used to detect duplicate transactions before import.
    /// </summary>
    /// <param name="hashes">The collection of hashes to check for existence.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. Default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains a hash set with only the hashes that already exist in the database.
    /// If none of the provided hashes exist, an empty hash set is returned.
    /// </returns>
    /// <remarks>
    /// This method is used during transaction import to identify duplicates before attempting insertion.
    /// It prevents unnecessary database operations and constraint violations by filtering out transactions
    /// that have already been imported. The hash is based on the SHA256 of the raw CNAB line.
    /// <para>
    /// Performance consideration: This method performs a single database query regardless of the number
    /// of hashes provided, making it efficient for bulk duplicate checks.
    /// </para>
    /// </remarks>
    Task<HashSet<string>> GetExistingHashesAsync(IEnumerable<string> hashes, CancellationToken cancellationToken = default);
}

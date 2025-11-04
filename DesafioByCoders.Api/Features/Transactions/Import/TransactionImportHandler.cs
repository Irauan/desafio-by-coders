using DesafioByCoders.Api.Features.Transactions.CnabParseStrategies;
using DesafioByCoders.Api.Handlers;
using DesafioByCoders.Api.Messages;

namespace DesafioByCoders.Api.Features.Transactions.Import;

/// <summary>
/// Imports a CNAB file, registers missing stores, records all valid transactions,
/// and returns a clear summary of what was imported and what failed per store.
/// </summary>
internal class TransactionImportHandler : IHandler<TransactionImportCommand, TransactionImportResult>
{
    private readonly IStoreRepository _storeRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILogger<TransactionImportHandler> _logger;
    private readonly TimeZoneInfo _cnabTimeZone;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionImportHandler"/> class.
    /// </summary>
    /// <param name="storeRepository">Repository for store data access operations.</param>
    /// <param name="transactionRepository">Repository for transaction data access operations.</param>
    /// <param name="logger">Logger instance for structured logging of import operations.</param>
    /// <remarks>
    /// The handler uses the "America/Sao_Paulo" timezone for converting CNAB local timestamps to UTC.
    /// This timezone is configured during construction and used throughout the import process.
    /// </remarks>
    public TransactionImportHandler(
        IStoreRepository storeRepository,
        ITransactionRepository transactionRepository,
        ILogger<TransactionImportHandler> logger
    )
    {
        _storeRepository = storeRepository;
        _transactionRepository = transactionRepository;
        _logger = logger;
        _cnabTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
    }

    /// <summary>
    /// Processes a CNAB file import by validating records, registering new stores, 
    /// persisting transactions, and providing detailed import statistics.
    /// </summary>
    /// <param name="request">The import command containing CNAB records to process.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous operation that contains the import result with:
    /// <list type="bullet">
    /// <item><description>Total count of successfully imported transactions</description></item>
    /// <item><description>Per-store summary of imported transaction counts</description></item>
    /// <item><description>Count and details of validation errors</description></item>
    /// <item><description>Count of duplicate transactions that were skipped</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// <para>
    /// The import process follows these steps:
    /// </para>
    /// <list type="number">
    /// <item><description><b>Parse and Validate:</b> Each CNAB line is parsed and validated. Invalid lines generate validation errors but don't stop processing.</description></item>
    /// <item><description><b>Extract Stores:</b> Unique stores are identified from valid records by their name (case-insensitive).</description></item>
    /// <item><description><b>Register New Stores:</b> Stores not already in the database are bulk inserted with their owner information.</description></item>
    /// <item><description><b>Create Transactions:</b> Valid records are converted to transaction entities with timezone-adjusted timestamps (local time in "America/Sao_Paulo" converted to UTC).</description></item>
    /// <item><description><b>Detect Duplicates:</b> Transactions are checked against existing records using SHA256 hash of the raw CNAB line. Duplicates are identified but not inserted.</description></item>
    /// <item><description><b>Bulk Insert:</b> New (non-duplicate) transactions are persisted to the database in a single bulk operation for performance.</description></item>
    /// <item><description><b>Generate Summary:</b> Import statistics are calculated including counts per store, total imported, duplicates skipped, and validation errors.</description></item>
    /// </list>
    /// <para>
    /// <b>Error Handling:</b> Validation errors for individual lines don't cause the entire import to fail. 
    /// The handler processes all valid records and returns validation errors in the result for caller inspection.
    /// </para>
    /// </remarks>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled via the <paramref name="cancellationToken"/>.</exception>
    /// <exception cref="TimeZoneNotFoundException">Thrown if the "America/Sao_Paulo" timezone is not found on the system (rare, only in misconfigured environments).</exception>
    public async Task<TransactionImportResult> HandleAsync(TransactionImportCommand request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting CNAB import with {LineCount} lines", request.CnabRecords.Count);

        var (parsedRecords, validationErrors) = ParseCnabRecords(request.CnabRecords);

        var (uniqueStoreNameList, cnabStores) = GetUniqueStores(parsedRecords);

        var databaseStores = await _storeRepository.GetExistentStores(uniqueStoreNameList);

        var storesToInsert = GetStoresToInsert(cnabStores, databaseStores);

        if (storesToInsert.Count > 0)
        {
            _logger.LogInformation("Inserting {NewStoreCount} new stores", storesToInsert.Count);

            await _storeRepository.BulkInsertAsync(storesToInsert, cancellationToken);

            _logger.LogInformation("Successfully inserted {NewStoreCount} new stores", storesToInsert.Count);
        }

        foreach (var insertedStore in storesToInsert)
        {
            databaseStores.Add(insertedStore.ToString(), insertedStore);
        }

        var allTransactions = new List<Transaction>(parsedRecords.Count);

        foreach (var record in parsedRecords)
        {
            var storeIdentifier = record.StoreName.ToLowerInvariant();

            var store = databaseStores[storeIdentifier];

            var occurredAtUtc = TimeZoneInfo.ConvertTimeToUtc(record.OccurredAtLocal, _cnabTimeZone);
            var transaction = Transaction.Create(
                store.Id,
                record.Type,
                record.Amount,
                occurredAtUtc,
                record.Cpf,
                record.Card,
                record.RawLine
            );

            transaction.SetStore(store);

            allTransactions.Add(transaction);
        }

        var transactionHashSet = allTransactions.Select(t => t.RawLineHash)
                                                .ToHashSet();

        _logger.LogDebug("Checking for duplicates among {TransactionCount} transactions", allTransactions.Count);

        var existingHashes = await _transactionRepository.GetExistingHashesAsync(transactionHashSet, cancellationToken);
        var transactionsToInsert = new List<Transaction>(allTransactions.Count);

        foreach (var transaction in allTransactions)
        {
            if (!existingHashes.Contains(transaction.RawLineHash))
            {
                transactionsToInsert.Add(transaction);
            }
        }

        var duplicateCount = allTransactions.Count - transactionsToInsert.Count;

        if (duplicateCount > 0)
        {
            _logger.LogInformation("Skipping {DuplicateCount} duplicate transactions", duplicateCount);
        }

        _logger.LogInformation("Inserting {TransactionCount} new transactions", transactionsToInsert.Count);

        await _transactionRepository.BulkInsertAsync(transactionsToInsert, cancellationToken);

        var summaryPerStore = transactionsToInsert.GroupBy(transaction => transaction.Store.ToString())
                                                  .Select(group => new TransactionImportResult.ImportSummaryPerStore(group.Key, group.Count()))
                                                  .ToList();

        _logger.LogInformation(
            "CNAB import completed successfully. Total: {TotalLines}, Imported: {ImportedCount}, Duplicates: {DuplicateCount}, Errors: {ErrorCount}, Stores: {StoreCount}",
            request.CnabRecords.Count,
            transactionsToInsert.Count,
            duplicateCount,
            validationErrors.Count,
            summaryPerStore.Count
        );

        return new TransactionImportResult(
            transactionsToInsert.Count,
            summaryPerStore,
            validationErrors.Count,
            validationErrors,
            duplicateCount
        );
    }

    /// <summary>
    /// Parses raw CNAB lines into structured records and collects validation errors.
    /// </summary>
    /// <param name="cnabRecords">List of raw CNAB text lines to parse.</param>
    /// <returns>
    /// A tuple containing:
    /// <list type="bullet">
    /// <item><description><b>ParsedRecords:</b> Successfully parsed and validated CNAB records ready for processing.</description></item>
    /// <item><description><b>ValidationErrors:</b> Collection of validation errors from lines that failed parsing or validation rules.</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method processes each CNAB line individually. Lines that fail parsing or validation
    /// generate error messages but don't stop processing of subsequent lines. This allows the import
    /// to process as many valid records as possible while reporting all problems.
    /// </para>
    /// <para>
    /// Common validation failures include:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Empty or null lines</description></item>
    /// <item><description>Lines with incorrect length (expected 80 characters)</description></item>
    /// <item><description>Invalid transaction type codes</description></item>
    /// <item><description>Malformed date or time fields</description></item>
    /// <item><description>Invalid numeric amounts</description></item>
    /// <item><description>Negative amounts (before sign adjustment)</description></item>
    /// </list>
    /// <para>
    /// Each validation error includes a code (e.g., "CNAB_INVALID_LENGTH") and a descriptive message
    /// with the line number for debugging purposes.
    /// </para>
    /// </remarks>
    private (List<CnabRecord> ParsedRecords, List<ValidationError> ValidationErrors) ParseCnabRecords(List<string> cnabRecords)
    {
        var parsedRecords = new List<CnabRecord>();
        var validationErrors = new List<ValidationError>();
        var cnabParser = new CnabParser(new Cnab80ParserStrategy());

        for (var i = 0; i < cnabRecords.Count; i++)
        {
            var lineNumber = i + 1; // 1-based line number
            var rawLine = cnabRecords[i];
            var parseResult = cnabParser.Parse(rawLine, lineNumber);
            
            if (parseResult.IsFailure)
            {
                var errors = (List<ValidationError>)parseResult;
                validationErrors.AddRange(errors);

                continue;
            }

            parsedRecords.Add((CnabRecord)parseResult);
        }
        
        if (validationErrors.Count > 0)
        {
            _logger.LogWarning("Found {ValidationErrorCount} validation errors during parsing", validationErrors.Count);
        }

        return (parsedRecords, validationErrors);
    }

    /// <summary>
    /// Extracts unique stores from parsed CNAB records using case-insensitive store name comparison.
    /// </summary>
    /// <param name="records">List of successfully parsed CNAB records containing store information.</param>
    /// <returns>
    /// A tuple containing:
    /// <list type="bullet">
    /// <item><description><b>UniqueStoreNameList:</b> HashSet of unique store identifiers (lowercase store names) for database lookup.</description></item>
    /// <item><description><b>CnabStores:</b> Dictionary mapping store identifiers to Store entities extracted from CNAB records.</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method identifies unique stores by their name in a case-insensitive manner.
    /// The store name is converted to lowercase to serve as a unique identifier.
    /// </para>
    /// <para>
    /// If multiple CNAB records reference the same store (by name), only the first occurrence
    /// is added to the CnabStores dictionary. Subsequent records with the same store name
    /// will reuse the existing Store entity.
    /// </para>
    /// <para>
    /// The returned HashSet is used to efficiently query the database for existing stores,
    /// while the Dictionary provides quick access to Store entities when creating transactions.
    /// </para>
    /// <para>
    /// <b>Example:</b> If the CNAB file contains transactions for "LOJA A", "Loja A", and "loja a",
    /// they will all be treated as the same store with identifier "loja a".
    /// </para>
    /// </remarks>
    private (HashSet<string> UniqueStoreNameList, Dictionary<string, Store> CnabStores) GetUniqueStores(List<CnabRecord> records)
    {
        var uniqueStoreNameList = new HashSet<string>();
        var cnabStores = new Dictionary<string, Store>();

        foreach (var record in records)
        {
            var store = Store.Create(record.StoreName, record.StoreOwner);

            uniqueStoreNameList.Add(store.ToString());
            cnabStores.TryAdd(store.ToString(), store);
        }
        
        _logger.LogInformation(
            "Parsed {ParsedRecordCount} valid records from {UniqueStoreCount} unique stores",
            records.Count,
            uniqueStoreNameList.Count
        );

        return (uniqueStoreNameList, cnabStores);
    }

    /// <summary>
    /// Determines which stores from the CNAB file need to be inserted into the database.
    /// </summary>
    /// <param name="cnabStores">Dictionary of stores extracted from the CNAB file, keyed by lowercase store name.</param>
    /// <param name="existingStores">Dictionary of stores already present in the database, keyed by lowercase store name.</param>
    /// <returns>
    /// A list of Store entities that exist in the CNAB file but not in the database, ready for insertion.
    /// Returns an empty list if all stores from the CNAB file already exist in the database.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method performs a set difference operation to identify new stores that need to be registered.
    /// It compares stores by their identifier (lowercase name) to determine which ones are missing from the database.
    /// </para>
    /// <para>
    /// <b>Performance Note:</b> The method uses Dictionary lookups (O(1)) rather than linear searches
    /// to efficiently identify missing stores, which is important when processing large CNAB files
    /// with many unique stores.
    /// </para>
    /// <para>
    /// <b>Idempotency:</b> If a CNAB file is imported multiple times, this method ensures that
    /// stores are only inserted once. Subsequent imports will find the stores already exist
    /// and skip the insertion step.
    /// </para>
    /// </remarks>
    private static List<Store> GetStoresToInsert(Dictionary<string, Store> cnabStores, Dictionary<string, Store> existingStores)
    {
        var storesToInsert = new List<Store>();

        foreach (var storeName in cnabStores.Keys)
        {
            if (existingStores.ContainsKey(storeName))
            {
                continue;
            }

            var store = cnabStores[storeName];

            storesToInsert.Add(store);
        }

        return storesToInsert;
    }
}
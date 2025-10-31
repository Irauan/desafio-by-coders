using DesafioByCoders.Api.Handlers;
using DesafioByCoders.Api.Messages;
using EFCore.BulkExtensions;

namespace DesafioByCoders.Api.Features.Transactions.Import;

internal class TransactionImportHandler : IHandler<TransactionImportCommand, TransactionImportResult>
{
    private readonly IStoreRepository _storeRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly TimeZoneInfo _cnabTimeZone;

    public TransactionImportHandler(IStoreRepository storeRepository, ITransactionRepository transactionRepository)
    {
        _storeRepository = storeRepository;
        _transactionRepository = transactionRepository;
        _cnabTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
    }

    public async Task<TransactionImportResult> HandleAsync(TransactionImportCommand request, CancellationToken cancellationToken = default)
    {
        var (parsedRecords, validationErrors) = ParseCnabRecords(request.CnabRecords);
        var (uniqueStoreNameList, cnabStores) = GetUniqueStores(parsedRecords);
        var databaseStores = await _storeRepository.GetExistentStores(uniqueStoreNameList);
        var storesToInsert = GetStoresToInsert(cnabStores, databaseStores);

        await _storeRepository.BulkInsertAsync(storesToInsert, cancellationToken);

        foreach (var insertedStore in storesToInsert)
        {
            databaseStores.Add(insertedStore.ToString(), insertedStore);
        }

        var transactions = new List<Transaction>();

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

            transactions.Add(transaction);
        }

        await _transactionRepository.BulkInsertAsync(transactions, cancellationToken);

        var summaryPerStore = transactions.GroupBy(transaction => transaction.Store.ToString())
                                          .Select(group => new TransactionImportResult.ImportSummaryPerStore(group.Key, group.Count()))
                                          .ToList();

        return new TransactionImportResult(
            parsedRecords.Count,
            summaryPerStore,
            validationErrors.Count,
            validationErrors
        );
    }

    private static (List<CnabRecord> ParsedRecords, List<ValidationError> ValidationErrors) ParseCnabRecords(List<string> cnabRecords)
    {
        var parsedRecords = new List<CnabRecord>();
        var validationErrors = new List<ValidationError>();

        foreach (var parseResult in cnabRecords.Select(CnabRecord.Create))
        {
            if (parseResult.IsFailure)
            {
                var errors = (List<ValidationError>)parseResult;
                validationErrors.AddRange(errors);

                continue;
            }

            parsedRecords.Add((CnabRecord)parseResult);
        }

        return (parsedRecords, validationErrors);
    }

    private static (HashSet<string> UniqueStoreNameList, Dictionary<string, Store> CnabStores) GetUniqueStores(List<CnabRecord> records)
    {
        var uniqueStoreNameList = new HashSet<string>();
        var cnabStores = new Dictionary<string, Store>();

        foreach (var record in records)
        {
            var store = Store.Create(record.StoreName, record.StoreOwner);

            uniqueStoreNameList.Add(store.ToString());
            cnabStores.TryAdd(store.ToString(), store);
        }

        return (uniqueStoreNameList, cnabStores);
    }

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
using DesafioByCoders.Api.Messages;

namespace DesafioByCoders.Api.Features.Transactions.Import;

public record TransactionImportResult(
    int TotalLinesImported,
    List<TransactionImportResult.ImportSummaryPerStore> ImportedSummaryPerStores,
    int TotalLinesInvalid,
    List<ValidationError> ValidationErrors,
    int TotalLinesDuplicate
)
{
    public record ImportSummaryPerStore(string StoreName, int Imported);
};
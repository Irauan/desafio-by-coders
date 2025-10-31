using DesafioByCoders.Api.Messages;

namespace DesafioByCoders.Api.Features.Transactions.Import;

internal record TransactionImportResult(
    int TotalLinesImported,
    List<TransactionImportResult.ImportSummaryPerStore> ImportSummaryPerStores,
    int TotalLinesInvalid,
    List<ValidationError> ValidationErrors
)
{
    public record ImportSummaryPerStore(string StoreName, int Imported);
};
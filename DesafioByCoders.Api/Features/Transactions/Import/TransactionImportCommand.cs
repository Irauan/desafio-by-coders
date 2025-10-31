using DesafioByCoders.Api.Handlers;

namespace DesafioByCoders.Api.Features.Transactions.Import;

internal record TransactionImportCommand(List<string> CnabRecords) : IRequest;
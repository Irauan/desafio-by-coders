using DesafioByCoders.Api.Handlers;

namespace DesafioByCoders.Api.Features.Transactions.Import;

public record TransactionImportCommand(List<string> CnabRecords) : IRequest;
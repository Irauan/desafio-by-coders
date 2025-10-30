namespace DesafioByCoders.Api.Features.Transactions;

internal static class TransactionTypeMetadata
{
    public static bool IsEntry(this TransactionType transactionType)
    {
        return transactionType switch
        {
            TransactionType.Boleto => false,
            TransactionType.Financing => false,
            TransactionType.Rent => false,
            _ => true
        };
    }

    public static int Sign(this TransactionType t) => t.IsEntry() ? 1 : -1;
}
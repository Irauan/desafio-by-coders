namespace DesafioByCoders.Api.Transactions;

public static class TransactionTypeMetadata
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
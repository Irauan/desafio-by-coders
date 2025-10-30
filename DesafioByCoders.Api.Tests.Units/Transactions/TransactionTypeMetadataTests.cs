using DesafioByCoders.Api.Transactions;

namespace DesafioByCoders.Api.Tests.Units.Transactions;

public class TransactionTypeMetadataTests
{
    [Theory]
    [InlineData(TransactionType.Debit, true)]
    [InlineData(TransactionType.Boleto, false)]
    [InlineData(TransactionType.Financing, false)]
    [InlineData(TransactionType.Credit, true)]
    [InlineData(TransactionType.LoanReceipt, true)]
    [InlineData(TransactionType.Sales, true)]
    [InlineData(TransactionType.TedReceipt, true)]
    [InlineData(TransactionType.DocReceipt, true)]
    [InlineData(TransactionType.Rent, false)]
    public void IsEntry_ValidTransactionTypes_ShouldReturnExpectedValue(TransactionType type, bool expectedIsEntry)
    {
        // Act
        var isEntry = type.IsEntry();

        // Assert
        Assert.Equal(expectedIsEntry, isEntry);
    }
    
    [Theory]
    [InlineData(TransactionType.Debit, 1)]
    [InlineData(TransactionType.Boleto, -1)]
    [InlineData(TransactionType.Financing, -1)]
    [InlineData(TransactionType.Credit, 1)]
    [InlineData(TransactionType.LoanReceipt, 1)]
    [InlineData(TransactionType.Sales, 1)]
    [InlineData(TransactionType.TedReceipt, 1)]
    [InlineData(TransactionType.DocReceipt, 1)]
    [InlineData(TransactionType.Rent, -1)]
    public void Sign_ValidaTransactionTypes_ShouldReturnExpectedValue(TransactionType type, int expectedSign)
    {
        // Act
        var sign = type.Sign();

        // Assert
        Assert.Equal(expectedSign, sign);
    }
}

using System.Security.Cryptography;
using System.Text;
using DesafioByCoders.Api.Features;
using DesafioByCoders.Api.Features.Transactions;

namespace DesafioByCoders.Api.Tests.Units.Features;

public class TransactionTests
{
    [Fact]
    public void Transaction_Create_ComputesSignedAmountAndHash_ForEntryType_WithoutUtcConversion()
    {
        var storeId = 42;
        var type = TransactionType.Debit;
        var amount = 150.75m;
        var occurredAtUtc = new DateTime(2024, 10, 5, 8, 15, 0, DateTimeKind.Utc);
        var cpf = "98765432100";
        var card = "999988887777";
        var rawLine = "sample raw line";

        var trx = Transaction.Create(storeId, type, amount, occurredAtUtc, cpf, card, rawLine);

        Assert.Equal(storeId, trx.StoreId);
        Assert.Null(trx.Store);
        Assert.Equal(type, trx.Type);
        Assert.Equal(amount, trx.Amount);
        Assert.Equal(amount, trx.SignedAmount);
        Assert.Equal(cpf, trx.Cpf);
        Assert.Equal(card, trx.Card);
        Assert.Equal(occurredAtUtc, trx.OccurredAtUtc);

        using var sha = SHA256.Create();

        var expectedHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(rawLine)));
        Assert.Equal(expectedHash, trx.RawLineHash);
    }

    [Fact]
    public void Transaction_Create_ComputesNegativeSignedAmount_ForExitType()
    {
        var trx = Transaction.Create(
            7,
            TransactionType.Boleto,
            200.00m,
            new DateTime(2023, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            "11122233344",
            "444433332222",
            "line");

        Assert.Equal(-200.00m, trx.SignedAmount);
    }

    [Fact]
    public void Transaction_Create_ZeroAmount_ProducesZeroSignedAmount()
    {
        var trx = Transaction.Create(
            9,
            TransactionType.Credit,
            0m,
            new DateTime(2022, 6, 15, 14, 0, 0, DateTimeKind.Utc),
            "55566677788",
            "111122223333",
            "raw");

        Assert.Equal(0m, trx.SignedAmount);
    }

    [Fact]
    public void Transaction_Create_PreservesUtcDateTime()
    {
        var occurredAtUtc = new DateTime(2023, 2, 1, 22, 30, 0, DateTimeKind.Utc);

        var trx = Transaction.Create(
            1,
            TransactionType.Credit,
            1.00m,
            occurredAtUtc,
            "00000000000",
            "000000000000",
            "another line");

        Assert.Equal(occurredAtUtc, trx.OccurredAtUtc);
    }

    [Fact]
    public void Transaction_Create_HashIsDeterministicForSameRawLine()
    {
        var raw = "same raw";

        var a = Transaction.Create(1, TransactionType.Credit, 10m, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), "1", "2", raw);
        var b = Transaction.Create(2, TransactionType.Debit, 20m, new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc), "3", "4", raw);

        Assert.Equal(a.RawLineHash, b.RawLineHash);
    }
}
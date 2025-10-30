using System.Security.Cryptography;
using System.Text;
using DesafioByCoders.Api.Features.Transactions;

namespace DesafioByCoders.Api.Features;

internal sealed class Transaction
{
    public long Id { get; private set; }
    public int StoreId { get; private set; }
    public Store Store { get; private set; } = null!;
    public TransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public decimal SignedAmount { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public string Cpf { get; private set; } = null!;
    public string Card { get; private set; } = null!;
    public string RawLineHash { get; private set; } = null!;

    private Transaction()
    {
    }

    private Transaction(
        int storeId,
        TransactionType type,
        decimal amount,
        decimal signedAmount,
        DateTime occurredAtUtc,
        string cpf,
        string card,
        string rawLineHash
    ) : this()
    {
        StoreId = storeId;
        Store = null!;
        Type = type;
        Amount = amount;
        SignedAmount = signedAmount;
        OccurredAtUtc = occurredAtUtc;
        Cpf = cpf;
        Card = card;
        RawLineHash = rawLineHash;
    }

    public static Transaction Create(
        int storeId,
        TransactionType type,
        decimal amount,
        DateTime occurredAtLocal,
        string cpf,
        string card,
        string rawLine
    )
    {
        var signedAmount = amount * type.Sign();

        var localZone = TimeZoneInfo.CreateCustomTimeZone(
            "UTC-3",
            TimeSpan.FromHours(-3),
            "UTC-3",
            "UTC-3"
        );

        var occurredAtUtc = TimeZoneInfo.ConvertTimeToUtc(occurredAtLocal, localZone);

        using var sha = SHA256.Create();

        var bytes = Encoding.UTF8.GetBytes(rawLine);
        var rawLineHash = Convert.ToHexString(sha.ComputeHash(bytes));

        return new Transaction(
            storeId,
            type,
            amount,
            signedAmount,
            occurredAtUtc,
            cpf,
            card,
            rawLineHash
        );
    }
}
using System.Security.Cryptography;
using System.Text;

namespace DesafioByCoders.Api.Features.Transactions;

internal sealed class Transaction
{
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

    public static Transaction Create(
        int storeId,
        TransactionType type,
        decimal amount,
        DateTime occurredAtUtc,
        string cpf,
        string card,
        string rawLine
    )
    {
        var signedAmount = amount * type.Sign();

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
    
    public void SetStore(Store store)
    {
        Store = store;
    }
}
using System.Globalization;
using DesafioByCoders.Api.Messages;

namespace DesafioByCoders.Api.Features.Transactions;

public sealed class CnabRecord
{
    public TransactionType Type { get; }
    public DateTime OccurredAtLocal { get; }
    public decimal Amount { get; }
    public string Cpf { get; }
    public string Card { get; }
    public string StoreOwner { get; }
    public string StoreName { get; }
    public string RawLine { get; }

    private CnabRecord(
        TransactionType type,
        DateTime occurredAtLocal,
        decimal amount,
        string cpf,
        string card,
        string storeOwner,
        string storeName,
        string rawLine
    )
    {
        Type = type;
        OccurredAtLocal = occurredAtLocal;
        Amount = amount;
        Cpf = cpf;
        Card = card;
        StoreOwner = storeOwner;
        StoreName = storeName;
        RawLine = rawLine;
    }

    // Create a record by parsing a CNAB fixed-width row.
    public static Result<CnabRecord> Create(string rawLine)
    {
        if (string.IsNullOrWhiteSpace(rawLine))
        {
            return new ValidationError("CNAB_EMPTY_LINE", "Line is empty.");
        }

        if (rawLine.Length < 81)
        {
            return new ValidationError("CNAB_INVALID_LENGTH", $"Line length invalid ({rawLine.Length}), expected >= 81.");
        }

        var typeText = rawLine[0..1];
        var dateText = rawLine[1..9];
        var amountText = rawLine[9..19];
        var cpfText = rawLine[19..30];
        var cardText = rawLine[30..42];
        var timeText = rawLine[42..48];
        var ownerText = rawLine[48..62];
        var storeText = rawLine[62..81];

        if (!TryParseInt(typeText, out var typeNumber))
        {
            return new ValidationError("CNAB_INVALID_TYPE", $"Invalid transaction type value '{typeText}'.");
        }

        var type = (TransactionType)typeNumber;

        if (!Enum.IsDefined(type))
        {
            return new ValidationError("CNAB_UNKNOWN_TYPE", $"Unknown transaction type {typeNumber}.");
        }

        if (!DateTime.TryParseExact(
                dateText,
                "yyyyMMdd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dateOnly
            ))
        {
            return new ValidationError("CNAB_INVALID_DATE", $"Invalid date '{dateText}'.");
        }

        if (!TimeSpan.TryParseExact(
                timeText,
                "hhmmss",
                CultureInfo.InvariantCulture,
                out var timeOfDay
            ))
        {
            return new ValidationError("CNAB_INVALID_TIME", $"Invalid time '{timeText}'.");
        }

        var occurredAtLocal = dateOnly.Add(timeOfDay);

        if (!TryParseInt(amountText, out var amountCents))
        {
            return new ValidationError("CNAB_INVALID_AMOUNT", $"Invalid amount '{amountText}'.");
        }

        var amountPositive = amountCents / 100.00m;

        if (amountPositive < 0)
        {
            return new ValidationError("CNAB_NEGATIVE_AMOUNT", "Amount must be non-negative before applying sign.");
        }

        var record = new CnabRecord(
            type,
            occurredAtLocal,
            amountPositive,
            cpfText.Trim(),
            cardText.Trim(),
            ownerText.Trim(),
            storeText.Trim(),
            rawLine
        );

        return record;
    }

    private static bool TryParseInt(string slice, out int value)
    {
        return int.TryParse(
            slice,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out value
        );
    }
}
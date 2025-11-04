using System.Globalization;
using DesafioByCoders.Api.Messages;

namespace DesafioByCoders.Api.Features.Transactions.CnabParseStrategies;

/// <summary>
/// Parses CNAB 80 fixed-width format transaction lines.
/// </summary>
/// <remarks>
/// <para>
/// This parser implements the Strategy pattern for parsing CNAB files with the following fixed-width format:
/// </para>
/// <list type="table">
/// <listheader>
/// <term>Position</term>
/// <term>Length</term>
/// <term>Field</term>
/// </listheader>
/// <item><description>0-1 (1 char)</description><description>Transaction Type</description></item>
/// <item><description>1-9 (8 chars)</description><description>Date (yyyyMMdd)</description></item>
/// <item><description>9-19 (10 chars)</description><description>Amount in cents</description></item>
/// <item><description>19-30 (11 chars)</description><description>CPF</description></item>
/// <item><description>30-42 (12 chars)</description><description>Card number</description></item>
/// <item><description>42-48 (6 chars)</description><description>Time (hhmmss)</description></item>
/// <item><description>48-62 (14 chars)</description><description>Store owner name</description></item>
/// <item><description>62-80 (18 chars)</description><description>Store name</description></item>
/// </list>
/// <para>
/// <b>Validation Rules:</b>
/// </para>
/// <list type="bullet">
/// <item><description>Line must be exactly 80 characters</description></item>
/// <item><description>Transaction type must be a valid numeric value from the <see cref="TransactionType"/> enum</description></item>
/// <item><description>Date must be in yyyyMMdd format and represent a valid date</description></item>
/// <item><description>Time must be in hhmmss format and represent a valid time</description></item>
/// <item><description>Amount must be a non-negative integer representing cents</description></item>
/// </list>
/// </remarks>
internal sealed class Cnab80ParserStrategy : ICnabParserStrategy
{
    /// <summary>
    /// Parses a CNAB 80 format line into a structured record.
    /// </summary>
    /// <param name="rawLine">The raw CNAB line text to parse (must be 80 characters).</param>
    /// <param name="lineNumber">The 1-based line number in the CNAB file (for debugging and error reporting).</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing either a parsed <see cref="CnabRecord"/> or validation errors.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The method performs the following validations in order:
    /// </para>
    /// <list type="number">
    /// <item><description>Checks if the line is empty or whitespace (error: CNAB_EMPTY_LINE)</description></item>
    /// <item><description>Validates line length is at least 80 characters (error: CNAB_INVALID_LENGTH)</description></item>
    /// <item><description>Validates transaction type is numeric (error: CNAB_INVALID_TYPE)</description></item>
    /// <item><description>Validates transaction type is a known enum value (error: CNAB_UNKNOWN_TYPE)</description></item>
    /// <item><description>Validates date format and validity (error: CNAB_INVALID_DATE)</description></item>
    /// <item><description>Validates time format and validity (error: CNAB_INVALID_TIME)</description></item>
    /// <item><description>Validates amount is numeric (error: CNAB_INVALID_AMOUNT)</description></item>
    /// <item><description>Validates amount is non-negative (error: CNAB_NEGATIVE_AMOUNT)</description></item>
    /// </list>
    /// <para>
    /// Text fields (CPF, Card, Owner, Store) are automatically trimmed of leading/trailing whitespace.
    /// </para>
    /// </remarks>
    public Result<CnabRecord> Parse(string rawLine, int lineNumber = 0)
    {
        if (string.IsNullOrWhiteSpace(rawLine))
        {
            return new ValidationError("CNAB_EMPTY_LINE", "Line is empty.");
        }

        if (rawLine.Length < 80)
        {
            return new ValidationError("CNAB_INVALID_LENGTH", $"Line {lineNumber}: Line length invalid ({rawLine.Length}), expected >= 80.");
        }

        var typeText = rawLine[0..1];
        var dateText = rawLine[1..9];
        var amountText = rawLine[9..19];
        var cpfText = rawLine[19..30];
        var cardText = rawLine[30..42];
        var timeText = rawLine[42..48];
        var ownerText = rawLine[48..62];
        var storeText = rawLine[62..80];

        if (!int.TryParse(
                typeText,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var typeNumber
            ))
        {
            return new ValidationError("CNAB_INVALID_TYPE", $"Line {lineNumber}: Invalid transaction type value '{typeText}'.");
        }

        var type = (TransactionType)typeNumber;

        if (!Enum.IsDefined(type))
        {
            return new ValidationError("CNAB_UNKNOWN_TYPE", $"Line {lineNumber}: Unknown transaction type {typeNumber}.");
        }

        if (!DateTime.TryParseExact(
                dateText,
                "yyyyMMdd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dateOnly
            ))
        {
            return new ValidationError("CNAB_INVALID_DATE", $"Line {lineNumber}: Invalid date '{dateText}'.");
        }

        if (!TimeSpan.TryParseExact(
                timeText,
                "hhmmss",
                CultureInfo.InvariantCulture,
                out var timeOfDay
            ))
        {
            return new ValidationError("CNAB_INVALID_TIME", $"Line {lineNumber}: Invalid time '{timeText}'.");
        }

        var occurredAtLocal = dateOnly.Add(timeOfDay);

        if (!long.TryParse(
                amountText,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var amountCents
            ))
        {
            return new ValidationError("CNAB_INVALID_AMOUNT", $"Line {lineNumber}: Invalid amount '{amountText}'.");
        }

        var amountPositive = amountCents / 100.00m;

        if (amountPositive < 0)
        {
            return new ValidationError("CNAB_NEGATIVE_AMOUNT", $"Line {lineNumber}: Amount must be non-negative before applying sign.");
        }

        var record = new CnabRecord(
            type,
            occurredAtLocal,
            amountPositive,
            cpfText.Trim(),
            cardText.Trim(),
            ownerText.Trim(),
            storeText.Trim(),
            rawLine,
            lineNumber
        );

        return record;
    }
}
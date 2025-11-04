namespace DesafioByCoders.Api.Features.Transactions.CnabParseStrategies;

/// <summary>
/// Represents a parsed CNAB (Centro Nacional de Automação Bancária) transaction record.
/// </summary>
/// <remarks>
/// <para>
/// This is a lightweight value type (struct) that contains the parsed data from a CNAB file line.
/// It is immutable after construction to ensure data integrity.
/// </para>
/// <para>
/// Use the <see cref="ICnabParserStrategy"/> implementations (e.g., <see cref="Cnab80ParserStrategy"/>) to create
/// instances of this struct from raw CNAB line text.
/// </para>
/// </remarks>
/// <param name="Type">The type of transaction (Debit, Credit, Boleto, etc.).</param>
/// <param name="OccurredAtLocal">The date and time when the transaction occurred in local time (America/Sao_Paulo).</param>
/// <param name="Amount">The transaction amount in decimal format (e.g., 123.45 for R$ 123.45).</param>
/// <param name="Cpf">The CPF (Brazilian tax ID) associated with the transaction.</param>
/// <param name="Card">The card number used for the transaction.</param>
/// <param name="StoreOwner">The name of the store owner.</param>
/// <param name="StoreName">The name of the store where the transaction occurred.</param>
/// <param name="RawLine">The original raw CNAB line text (preserved for hash calculation and auditing).</param>
/// <param name="LineNumber">The 1-based line number in the CNAB file (for debugging and error reporting).</param>
internal readonly record struct CnabRecord(
    TransactionType Type,
    DateTime OccurredAtLocal,
    decimal Amount,
    string Cpf,
    string Card,
    string StoreOwner,
    string StoreName,
    string RawLine,
    int LineNumber
);
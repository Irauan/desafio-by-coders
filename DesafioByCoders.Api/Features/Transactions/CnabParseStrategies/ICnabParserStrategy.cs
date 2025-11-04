using DesafioByCoders.Api.Messages;

namespace DesafioByCoders.Api.Features.Transactions.CnabParseStrategies;

/// <summary>
/// Defines the contract for parsing CNAB (Centro Nacional de Automação Bancária) file lines.
/// </summary>
/// <remarks>
/// This interface follows the Strategy pattern, allowing different parsing implementations
/// for various CNAB formats or versions without changing the client code.
/// </remarks>
internal interface ICnabParserStrategy
{
    /// <summary>
    /// Parses a raw CNAB line into a structured record.
    /// </summary>
    /// <param name="rawLine">The raw CNAB line text to parse.</param>
    /// <param name="lineNumber">The 1-based line number in the CNAB file (for debugging and error reporting).</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing either:
    /// <list type="bullet">
    /// <item><description>A successfully parsed <see cref="CnabRecord"/> on success</description></item>
    /// <item><description>A list of <see cref="ValidationError"/> on failure</description></item>
    /// </list>
    /// </returns>
    Result<CnabRecord> Parse(string rawLine, int lineNumber);
}

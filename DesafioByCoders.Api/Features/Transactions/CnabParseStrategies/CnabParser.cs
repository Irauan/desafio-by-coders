namespace DesafioByCoders.Api.Features.Transactions.CnabParseStrategies;

/// <summary>
/// Main CNAB parser that delegates parsing to a specific strategy implementation.
/// </summary>
/// <remarks>
/// This class follows the Strategy pattern, allowing different CNAB format parsers
/// to be used interchangeably.
/// </remarks>
internal class CnabParser
{
    private readonly ICnabParserStrategy _strategy;

    /// <summary>
    /// Initializes a new instance of the <see cref="CnabParser"/> class.
    /// </summary>
    /// <param name="strategy">The parsing strategy to use for CNAB lines.</param>
    public CnabParser(ICnabParserStrategy strategy)
    {
        _strategy = strategy;
    }
    
    /// <summary>
    /// Parses a raw CNAB line into a structured record.
    /// </summary>
    /// <param name="rawLine">The raw CNAB line text to parse.</param>
    /// <param name="lineNumber">The 1-based line number in the CNAB file (for debugging and error reporting).</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing either a successfully parsed <see cref="CnabRecord"/> 
    /// or a list of <see cref="ValidationError"/> if parsing failed.
    /// </returns>
    public Result<CnabRecord> Parse(string rawLine, int lineNumber)
    {
        return _strategy.Parse(rawLine, lineNumber);
    }
}
using System.Net;
using Asp.Versioning;
using DesafioByCoders.Api.Handlers;
using DesafioByCoders.Api.Messages;
using Microsoft.AspNetCore.Mvc;

namespace DesafioByCoders.Api.Features.Transactions.Import;

/// <summary>
/// Handles the CNAB transaction file import workflow.
/// </summary>
/// <remarks>
/// This endpoint accepts a CNAB flat file uploaded as multipart/form-data and imports
/// valid transactions, returning a per-store import summary. If one or more lines are invalid,
/// the endpoint returns a 207 Multi-Status payload containing both the success summary and the
/// validation errors.
/// </remarks>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/transactions")]
[ApiController]
public class TransactionImportController : ControllerBase
{
    private readonly IHandler<TransactionImportCommand, TransactionImportResult> _handler;

    /// <summary>
    /// Creates a new instance of <see cref="TransactionImportController"/>.
    /// </summary>
    /// <param name="handler">The import use case handler.</param>
    public TransactionImportController(IHandler<TransactionImportCommand, TransactionImportResult> handler)
    {
        _handler = handler;
    }

    /// <summary>
    /// Uploads and imports a CNAB file with transactions.
    /// </summary>
    /// <param name="file">The CNAB file (multipart/form-data) to be processed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// 200 OK when all lines are valid with an import summary; 207 Multi-Status when some lines are invalid
    /// with both success and error sections; 400 Bad Request when the file is empty or missing.
    /// </returns>
    [HttpPost("import", Name = "transaction-import")]
    [Tags("Transaction")]
    [EndpointDescription(
        "Accepts a CNAB fixed-width file uploaded as multipart/form-data (form field 'file'). " +
        "Imports valid transactions and returns a per-store summary. Returns 200 when all lines are valid; " +
        "207 Multi-Status with both summary and validation errors when some lines are invalid; " +
        "400 when the file is missing or empty."
    )]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType<TransactionImportOkResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<TransactionImportMultiStatusResponse>(StatusCodes.Status207MultiStatus)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Upload([FromForm] IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file.Length == 0)
        {
            return BadRequest("File is empty.");
        }

        var command = await CreateTransactionImportCommand(file, cancellationToken);

        var result = await _handler.HandleAsync(command, cancellationToken);

        if (result.TotalLinesInvalid == 0)
        {
            return Ok(new TransactionImportOkResponse(200, result.TotalLinesImported, result.ImportedSummaryPerStores));
        }

        if (result.TotalLinesImported == 0)
        {
            return StatusCode(
                (int)HttpStatusCode.UnprocessableEntity,
                new TransactionImportErrorResponse((int)HttpStatusCode.UnprocessableEntity, result.TotalLinesInvalid, result.ValidationErrors)
            );
        }

        return StatusCode(
            (int)HttpStatusCode.MultiStatus,
            new TransactionImportMultiStatusResponse(
                [
                    new TransactionImportOkResponse((int)HttpStatusCode.MultiStatus, result.TotalLinesImported, result.ImportedSummaryPerStores),
                    new TransactionImportErrorResponse((int)HttpStatusCode.MultiStatus, result.TotalLinesInvalid, result.ValidationErrors)
                ]
            )
        );
    }

    private static async Task<TransactionImportCommand> CreateTransactionImportCommand(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();

        using var reader = new StreamReader(stream);

        var cnabRows = new List<string>();

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(cancellationToken) ?? "";

            cnabRows.Add(line);
        }

        var command = new TransactionImportCommand(cnabRows);

        return command;
    }

    public sealed record TransactionImportOkResponse(int Status, int TotalImportedLines, List<TransactionImportResult.ImportSummaryPerStore> ImportedSummaryPerStores);

    /// <summary>
    /// Error response for transaction import operations.
    /// </summary>
    /// <param name="Status">HTTP status code for the response.</param>
    /// <param name="TotalInvalidLines">Total number of invalid lines found.</param>
    /// <param name="Errors">List of validation errors.</param>
    public sealed record TransactionImportErrorResponse(int Status, int TotalInvalidLines, List<ValidationError> Errors);

    /// <summary>
    /// Aggregated multi-status response containing both success and error sections.
    /// </summary>
    /// <param name="Results">The list of result payloads for the request.</param>
    public sealed record TransactionImportMultiStatusResponse(List<object> Results);
}
using System.Text;
using DesafioByCoders.Api.Features.Transactions;
using DesafioByCoders.Api.Features.Transactions.Import;
using DesafioByCoders.Api.Handlers;
using DesafioByCoders.Api.Messages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DesafioByCoders.Api.Tests.Units.Features.Transactions.Import;

public class TransactionImportControllerTests
{
    [Fact]
    public async Task Upload_WithEmptyFile_ReturnsBadRequest()
    {
        var controller = CreateControllerWithResult(null);
        
        var emptyFile = CreateFormFile("", "empty.txt");

        var result = await controller.Upload(emptyFile, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        
        Assert.Equal("File is empty.", badRequest.Value);
    }

    [Fact]
    public async Task Upload_WithAllValidLines_ReturnsOkWithSummary()
    {
        var summary = MakeSummary(("store a", 2));
        
        var response = new TransactionImportResult(2, summary, 0, new List<ValidationError>());
        
        var controller = CreateControllerWithResult(response);

        var file = CreateFormFile("some-content");

        var result = await controller.Upload(file, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        
        var payload = Assert.IsType<TransactionImportController.TransactionImportOkResponse>(ok.Value);
        
        Assert.Equal(200, payload.Status);
        Assert.Equal(2, payload.TotalImportedLines);
        Assert.Equal(summary, payload.ImportedSummaryPerStores);
    }

    [Fact]
    public async Task Upload_WithOnlyInvalidLines_ReturnsUnprocessableEntity()
    {
        var errors = MakeErrors(("CNAB_INVALID_LENGTH", "Invalid length"));
        
        var response = new TransactionImportResult(0, new List<TransactionImportResult.ImportSummaryPerStore>(), 1, errors);
        
        var controller = CreateControllerWithResult(response);

        var file = CreateFormFile("x");

        var result = await controller.Upload(file, CancellationToken.None);

        var obj = Assert.IsType<ObjectResult>(result);
        
        Assert.Equal(422, obj.StatusCode);
        
        var payload = Assert.IsType<TransactionImportController.TransactionImportErrorResponse>(obj.Value);
        
        Assert.Equal(422, payload.Status);
        Assert.Equal(1, payload.TotalInvalidLines);
        Assert.Equal(errors, payload.Errors);
    }

    [Fact]
    public async Task Upload_WithSomeValidSomeInvalid_ReturnsMultiStatus()
    {
        var summary = MakeSummary(("store a", 1));
        
        var errors = MakeErrors(("CNAB_INVALID_TYPE", "Invalid type"));
        
        var response = new TransactionImportResult(1, summary, 1, errors);
        
        var controller = CreateControllerWithResult(response);
        
        var file = CreateFormFile("x");
        
        var result = await controller.Upload(file, CancellationToken.None);

        var obj = Assert.IsType<ObjectResult>(result);
        
        Assert.Equal(207, obj.StatusCode);
        
        var payload = Assert.IsType<TransactionImportController.TransactionImportMultiStatusResponse>(obj.Value);
        
        Assert.Equal(2, payload.Results.Count);
        
        var okPart = Assert.IsType<TransactionImportController.TransactionImportOkResponse>(payload.Results[0]);
        var errPart = Assert.IsType<TransactionImportController.TransactionImportErrorResponse>(payload.Results[1]);
        
        Assert.Equal(207, okPart.Status);
        Assert.Equal(1, okPart.TotalImportedLines);
        Assert.Equal(summary, okPart.ImportedSummaryPerStores);
        
        Assert.Equal(207, errPart.Status);
        Assert.Equal(1, errPart.TotalInvalidLines);
        Assert.Equal(errors, errPart.Errors);
    }

    // Helpers
    
    private static IFormFile CreateFormFile(string content, string fileName = "cnab.txt")
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        
        var stream = new MemoryStream(bytes);
        
        var formFile = new FormFile(stream, 0, stream.Length, "file", fileName);
        
        return formFile;
    }

    private static TransactionImportController CreateControllerWithResult(TransactionImportResult? result)
    {
        var handlerMock = new Mock<IHandler<TransactionImportCommand, TransactionImportResult>>();
        
        if (result is not null)
        {
            handlerMock
                .Setup(h => h.HandleAsync(It.IsAny<TransactionImportCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);
        }
        
        var controller = new TransactionImportController(handlerMock.Object);
        
        return controller;
    }

    private static List<TransactionImportResult.ImportSummaryPerStore> MakeSummary(params (string Store, int Imported)[] entries)
    {
        var list = new List<TransactionImportResult.ImportSummaryPerStore>();
        
        foreach (var entry in entries)
        {
            list.Add(new TransactionImportResult.ImportSummaryPerStore(entry.Store, entry.Imported));
        }
        
        return list;
    }

    private static List<ValidationError> MakeErrors(params (string Code, string Message)[] errors)
    {
        var list = new List<ValidationError>();
        
        foreach (var e in errors)
        {
            list.Add(new ValidationError(e.Code, e.Message));
        }
        
        return list;
    }
}
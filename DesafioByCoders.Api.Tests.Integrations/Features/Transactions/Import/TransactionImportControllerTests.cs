using System.Net;
using System.Net.Http.Headers;
using DesafioByCoders.Api.Features.Transactions;
using DesafioByCoders.Api.Tests.Integrations.Infrastructure;

namespace DesafioByCoders.Api.Tests.Integrations.Features.Transactions.Import;

public class TransactionImportControllerTests : IClassFixture<ApiFixture>,
                                                IAsyncLifetime
{
    private readonly ApiFixture _fixture;

    public TransactionImportControllerTests(ApiFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await using var dbContext = _fixture.CreateDbContext<TransactionDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Upload_WithOkCnab_ShouldReturnOk()
    {
        using var content = await BuildTransactionImportContent("Ok-CNAB.txt");

        var response = await _fixture.Client.PostAsync("/api/v1/transactions/import", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<MultipartFormDataContent> BuildTransactionImportContent(string fileName)
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "Features",
            "Transactions",
            "Import",
            fileName
        );

        var content = new MultipartFormDataContent();
        var bytes = await File.ReadAllBytesAsync(path);
        var fileContent = new ByteArrayContent(bytes);

        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

        content.Add(fileContent, "file", fileName);

        return content;
    }
}
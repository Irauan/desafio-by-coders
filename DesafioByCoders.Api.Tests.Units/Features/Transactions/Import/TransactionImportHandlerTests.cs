﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DesafioByCoders.Api.Features;
using DesafioByCoders.Api.Features.Transactions;
using DesafioByCoders.Api.Features.Transactions.Import;
using DesafioByCoders.Api.Messages;
using Moq;
using Xunit;

namespace DesafioByCoders.Api.Tests.Units.Features.Transactions.Import;

public class TransactionImportHandlerTests
{
    private readonly Mock<IStoreRepository> storeRepositoryMock;
    private readonly Mock<ITransactionRepository> transactionRepositoryMock;
    private readonly TransactionImportHandler handler;
    private readonly List<Store> capturedStores;
    private readonly List<Transaction> capturedTransactions;

    public TransactionImportHandlerTests()
    {
        storeRepositoryMock = new Mock<IStoreRepository>();
        
        transactionRepositoryMock = new Mock<ITransactionRepository>();
        
        capturedStores = new List<Store>();
        
        capturedTransactions = new List<Transaction>();

        storeRepositoryMock
            .Setup(r => r.GetExistentStores(It.IsAny<HashSet<string>>()))
            .ReturnsAsync(new Dictionary<string, Store>());
        
        storeRepositoryMock
            .Setup(r => r.BulkInsertAsync(It.IsAny<IEnumerable<Store>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<Store>, CancellationToken>((stores, _) => capturedStores.AddRange(stores))
            .Returns(Task.CompletedTask);
        
        transactionRepositoryMock
            .Setup(r => r.BulkInsertAsync(It.IsAny<IEnumerable<Transaction>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<Transaction>, CancellationToken>((transactions, _) => capturedTransactions.AddRange(transactions))
            .Returns(Task.CompletedTask);

        handler = new TransactionImportHandler(storeRepositoryMock.Object, transactionRepositoryMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WithAllValidRecords_InsertsStoresAndTransactionsAndSummarizesPerStore()
    {
        var lineOne = BuildLine("1", "20240101", "0000000100", "12345678901", "111122223333", "101500", "ALICE", "MERCADO A");
        
        var lineTwo = BuildLine("4", "20240101", "0000000200", "12345678901", "111122223333", "111600", "BOB", "FARMACIA B");
        
        var command = new TransactionImportCommand(new List<string> { lineOne, lineTwo });

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(2, result.TotalLinesImported);
        Assert.Equal(0, result.TotalLinesInvalid);
        Assert.Empty(result.ValidationErrors);

        Assert.Equal(2, capturedStores.Count);
        Assert.Equal(new[] { "mercado a", "farmacia b" }.OrderBy(x => x), capturedStores.Select(s => s.ToString()).OrderBy(x => x));

        Assert.Equal(2, capturedTransactions.Count);

        var summary = result.ImportSummaryPerStores.OrderBy(x => x.StoreName).ToList();
        
        Assert.Equal(2, summary.Count);
        Assert.Equal("farmacia b", summary[0].StoreName);
        Assert.Equal(1, summary[0].Imported);
        Assert.Equal("mercado a", summary[1].StoreName);
        Assert.Equal(1, summary[1].Imported);
    }

    [Fact]
    public async Task HandleAsync_WithSomeInvalidRecords_ReturnsValidationErrorsAndSkipsInvalids()
    {
        var validLine = BuildLine("1", "20240101", "0000000100", "12345678901", "111122223333", "101500", "ALICE", "MERCADO A");
        
        var invalidLengthLine = new string('0', 80);
        
        var invalidTypeLine = BuildLine("X", "20240101", "0000000100", "12345678901", "111122223333", "101500", "ALICE", "MERCADO A");
        
        var command = new TransactionImportCommand(new List<string> { validLine, invalidLengthLine, invalidTypeLine });

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(1, result.TotalLinesImported);
        Assert.Equal(2, result.TotalLinesInvalid);
        Assert.Equal(2, result.ValidationErrors.Count);
        Assert.Contains(result.ValidationErrors, e => e.Code == "CNAB_INVALID_LENGTH");
        Assert.Contains(result.ValidationErrors, e => e.Code == "CNAB_INVALID_TYPE");

        Assert.Single(capturedTransactions);
        Assert.Single(result.ImportSummaryPerStores);
        Assert.Equal("mercado a", result.ImportSummaryPerStores[0].StoreName);
        Assert.Equal(1, result.ImportSummaryPerStores[0].Imported);
    }

    [Fact]
    public async Task HandleAsync_WithExistingStores_DoesNotInsertExistingStores()
    {
        var existingStore = Store.Create("MERCADO A", "ALICE");
        
        var existentStores = new Dictionary<string, Store>
        {
            { existingStore.ToString(), existingStore }
        };

        storeRepositoryMock
            .Setup(r => r.GetExistentStores(It.IsAny<HashSet<string>>()))
            .ReturnsAsync(existentStores);

        var lineOne = BuildLine("1", "20240101", "0000000100", "12345678901", "111122223333", "101500", "ALICE", "MERCADO A");
        
        var lineTwo = BuildLine("1", "20240102", "0000000200", "22222222222", "444455556666", "121500", "ALICE", "MERCADO A");
        
        var command = new TransactionImportCommand(new List<string> { lineOne, lineTwo });

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(2, result.TotalLinesImported);
        Assert.Empty(capturedStores);
        Assert.Equal(2, capturedTransactions.Count);
        Assert.Single(result.ImportSummaryPerStores);
        Assert.Equal("mercado a", result.ImportSummaryPerStores[0].StoreName);
        Assert.Equal(2, result.ImportSummaryPerStores[0].Imported);
    }

    [Fact]
    public async Task HandleAsync_WithDuplicateStoreIdentifiers_InsertsStoreOnce()
    {
        var lineOne = BuildLine("1", "20240101", "0000000100", "12345678901", "111122223333", "101500", "ALICE", "MERCADO A");
        
        var lineTwo = BuildLine("4", "20240101", "0000000200", "12345678901", "111122223333", "111600", "ALICE", "MERCADO A");
        
        var command = new TransactionImportCommand(new List<string> { lineOne, lineTwo });

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(2, result.TotalLinesImported);
        Assert.Single(capturedStores);
        Assert.Equal("mercado a", capturedStores[0].ToString());
        Assert.Equal(2, capturedTransactions.Count);
        Assert.Single(result.ImportSummaryPerStores);
        Assert.Equal(2, result.ImportSummaryPerStores[0].Imported);
    }

    private static string BuildLine(
        string type,
        string date,
        string amount,
        string cpf,
        string card,
        string time,
        string owner,
        string store)
    {
        var ownerPadded = owner.PadRight(14, ' ');
        
        var storePadded = store.PadRight(19, ' ');
        
        return string.Concat(
            type,
            date,
            amount,
            cpf,
            card,
            time,
            ownerPadded,
            storePadded);
    }
}
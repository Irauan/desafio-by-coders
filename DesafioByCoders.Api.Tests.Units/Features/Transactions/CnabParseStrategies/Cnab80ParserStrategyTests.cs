using DesafioByCoders.Api.Features.Transactions;
using DesafioByCoders.Api.Features.Transactions.CnabParseStrategies;
using DesafioByCoders.Api.Messages;

namespace DesafioByCoders.Api.Tests.Units.Features.Transactions.CnabParseStrategies;

public class Cnab80ParserStrategyTests
{
    private readonly ICnabParserStrategy _parser = new Cnab80ParserStrategy();

    [Fact]
    public void Parse_WithValidLine_ReturnsSuccessAndParsesAllFields()
    {
        // Arrange
        var type = 1; // Debit
        var date = "20190301";
        var amount = "0000012345"; // 123.45
        var cpf = "12345678901";
        var card = "123456789012";
        var time = "123000"; // 12:30:00
        var owner = "JOSE DA SILVA";
        var store = "PADARIA DO ZE";
        var line = BuildLine(type.ToString(), date, amount, cpf, card, time, owner, store);
        var lineNumber = 1;

        // Act
        var result = _parser.Parse(line, lineNumber);

        // Assert
        Assert.True(result.IsSuccess);
        var record = (CnabRecord)result;
        Assert.Equal(TransactionType.Debit, record.Type);
        Assert.Equal(new DateTime(2019, 3, 1, 12, 30, 0), record.OccurredAtLocal);
        Assert.Equal(123.45m, record.Amount);
        Assert.Equal(cpf, record.Cpf);
        Assert.Equal(card, record.Card);
        Assert.Equal(owner, record.StoreOwner);
        Assert.Equal(store, record.StoreName);
        Assert.Equal(line, record.RawLine);
        Assert.Equal(lineNumber, record.LineNumber);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_WithEmptyOrWhitespaceLine_ReturnsEmptyLineFailure(string? line)
    {
        // Act
        var result = _parser.Parse(line ?? string.Empty, 1);

        // Assert
        Assert.True(result.IsFailure);
        var errors = (List<ValidationError>)result;
        Assert.Contains(errors, e => e.Code == "CNAB_EMPTY_LINE");
    }

    [Fact]
    public void Parse_WithShortLine_ReturnsInvalidLengthFailure()
    {
        // Arrange
        var line = new string('0', 79);

        // Act
        var result = _parser.Parse(line, 1);

        // Assert
        Assert.True(result.IsFailure);
        var errors = (List<ValidationError>)result;
        Assert.Contains(errors, e => e.Code == "CNAB_INVALID_LENGTH");
        Assert.Contains(errors, e => e.Message.Contains("79"));
    }

    [Fact]
    public void Parse_WithNonNumericType_ReturnsInvalidTypeFailure()
    {
        // Arrange
        var line = BuildLine("X", "20190301", "0000000001", "12345678901", "123456789012", "000000", "OWNER", "STORE");

        // Act
        var result = _parser.Parse(line, 1);

        // Assert
        Assert.True(result.IsFailure);
        var errors = (List<ValidationError>)result;
        Assert.Contains(errors, e => e.Code == "CNAB_INVALID_TYPE");
    }

    [Fact]
    public void Parse_WithUnknownType_ReturnsUnknownTypeFailure()
    {
        // Arrange (type 0 is Unknown per enum, but considered invalid for input)
        var line = BuildLine("0", "20190301", "0000000001", "12345678901", "123456789012", "000000", "OWNER", "STORE");

        // Act
        var result = _parser.Parse(line, 1);

        // Assert
        Assert.True(result.IsFailure);
        var errors = (List<ValidationError>)result;
        Assert.Contains(errors, e => e.Code == "CNAB_UNKNOWN_TYPE");
    }

    [Fact]
    public void Parse_WithInvalidDate_ReturnsInvalidDateFailure()
    {
        // Arrange
        var line = BuildLine("1", "20190230", "0000000001", "12345678901", "123456789012", "000000", "OWNER", "STORE");

        // Act
        var result = _parser.Parse(line, 1);

        // Assert
        Assert.True(result.IsFailure);
        var errors = (List<ValidationError>)result;
        Assert.Contains(errors, e => e.Code == "CNAB_INVALID_DATE");
    }

    [Fact]
    public void Parse_WithInvalidTime_ReturnsInvalidTimeFailure()
    {
        // Arrange
        var line = BuildLine("1", "20190301", "0000000001", "12345678901", "123456789012", "246199", "OWNER", "STORE");

        // Act
        var result = _parser.Parse(line, 1);

        // Assert
        Assert.True(result.IsFailure);
        var errors = (List<ValidationError>)result;
        Assert.Contains(errors, e => e.Code == "CNAB_INVALID_TIME");
    }

    [Fact]
    public void Parse_WithNonNumericAmount_ReturnsInvalidAmountFailure()
    {
        // Arrange
        var line = BuildLine("1", "20190301", "00000ABCDE", "12345678901", "123456789012", "000000", "OWNER", "STORE");

        // Act
        var result = _parser.Parse(line, 1);

        // Assert
        Assert.True(result.IsFailure);
        var errors = (List<ValidationError>)result;
        Assert.Contains(errors, e => e.Code == "CNAB_INVALID_AMOUNT");
    }

    [Fact]
    public void Parse_WithNegativeAmount_ReturnsNegativeAmountFailure()
    {
        // Arrange
        var line = BuildLine("1", "20190301", "-000000010", "12345678901", "123456789012", "000000", "OWNER", "STORE");

        // Act
        var result = _parser.Parse(line, 1);

        // Assert
        Assert.True(result.IsFailure);
        var errors = (List<ValidationError>)result;
        Assert.Contains(errors, e => e.Code == "CNAB_NEGATIVE_AMOUNT");
    }

    [Fact]
    public void Parse_WithAllTransactionTypes_ParsesCorrectly()
    {
        // Test each valid transaction type
        var testCases = new[]
        {
            (Type: TransactionType.Debit, Code: "1"),
            (Type: TransactionType.Boleto, Code: "2"),
            (Type: TransactionType.Financing, Code: "3"),
            (Type: TransactionType.Credit, Code: "4"),
            (Type: TransactionType.LoanReceipt, Code: "5"),
            (Type: TransactionType.Sales, Code: "6"),
            (Type: TransactionType.TedReceipt, Code: "7"),
            (Type: TransactionType.DocReceipt, Code: "8"),
            (Type: TransactionType.Rent, Code: "9")
        };

        foreach (var testCase in testCases)
        {
            // Arrange
            var line = BuildLine(testCase.Code, "20190301", "0000000100", "12345678901", "123456789012", "120000", "OWNER", "STORE");

            // Act
            var result = _parser.Parse(line, 1);

            // Assert
            Assert.True(result.IsSuccess, $"Failed to parse transaction type {testCase.Type}");
            var record = (CnabRecord)result;
            Assert.Equal(testCase.Type, record.Type);
        }
    }

    [Fact]
    public void Parse_WithPaddedTextFields_TrimsWhitespace()
    {
        // Arrange
        var line = BuildLine("1", "20190301", "0000000100", "12345678901", "123456789012", "120000", "  OWNER  ", "  STORE  ");

        // Act
        var result = _parser.Parse(line, 1);

        // Assert
        Assert.True(result.IsSuccess);
        var record = (CnabRecord)result;
        Assert.Equal("OWNER", record.StoreOwner);
        Assert.Equal("STORE", record.StoreName);
    }

    [Fact]
    public void Parse_WithZeroAmount_ParsesSuccessfully()
    {
        // Arrange
        var line = BuildLine("1", "20190301", "0000000000", "12345678901", "123456789012", "120000", "OWNER", "STORE");

        // Act
        var result = _parser.Parse(line, 1);

        // Assert
        Assert.True(result.IsSuccess);
        var record = (CnabRecord)result;
        Assert.Equal(0m, record.Amount);
    }

    [Fact]
    public void Parse_WithLargeAmount_ParsesCorrectly()
    {
        // Arrange
        var line = BuildLine("1", "20190301", "9999999999", "12345678901", "123456789012", "120000", "OWNER", "STORE");

        // Act
        var result = _parser.Parse(line, 1);

        // Assert
        Assert.True(result.IsSuccess);
        var record = (CnabRecord)result;
        Assert.Equal(99999999.99m, record.Amount);
    }

    [Fact]
    public void Parse_WithExactly80Characters_ParsesSuccessfully()
    {
        // Arrange
        var line = BuildLine("1", "20190301", "0000000100", "12345678901", "123456789012", "120000", "OWNER", "STORE");

        // Act
        var result = _parser.Parse(line, 1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(80, line.Length);
        var record = (CnabRecord)result;
        Assert.Equal(TransactionType.Debit, record.Type);
    }

    [Fact]
    public void Parse_WithMoreThan80Characters_ParsesFirst80Characters()
    {
        // Arrange - build line with extra characters at the end
        var line = BuildLine("1", "20190301", "0000000100", "12345678901", "123456789012", "120000", "OWNER", "STORE") + "EXTRA";

        // Act
        var result = _parser.Parse(line, 1);

        // Assert - should still parse successfully, uses first 80 chars
        Assert.True(result.IsSuccess);
        var record = (CnabRecord)result;
        Assert.Equal(TransactionType.Debit, record.Type);
        Assert.Equal("STORE", record.StoreName); // Not "STOREEXTRA"
    }

    [Fact]
    public void Parse_WithCustomParserStrategy_UsesProvidedParser()
    {
        // Arrange
        var customParser = new Cnab80ParserStrategy();
        var line = BuildLine("2", "20190301", "0000000200", "12345678901", "123456789012", "120000", "OWNER", "STORE");

        // Act
        var result = customParser.Parse(line, 2);

        // Assert
        Assert.True(result.IsSuccess);
        var record = (CnabRecord)result;
        Assert.Equal(TransactionType.Boleto, record.Type);
        Assert.Equal(2.00m, record.Amount);
        Assert.Equal(2, record.LineNumber);
    }

    [Fact]
    public void CnabRecord_IsStruct_HasValueSemantics()
    {
        // Arrange
        var line = BuildLine("1", "20190301", "0000000100", "12345678901", "123456789012", "120000", "OWNER", "STORE");
        var result = _parser.Parse(line, 1);
        var record1 = (CnabRecord)result;
        
        // Create another record with same values
        var record2 = new CnabRecord(
            TransactionType.Debit,
            new DateTime(2019, 3, 1, 12, 0, 0),
            1.00m,
            "12345678901",
            "123456789012",
            "OWNER",
            "STORE",
            line,
            1
        );

        // Assert - structs with same values should be equal
        Assert.Equal(record1, record2);
        Assert.True(record1 == record2);
    }

    [Fact]
    public void Parse_WithDifferentLineNumbers_StoresCorrectLineNumber()
    {
        // Arrange
        var line1 = BuildLine("1", "20190301", "0000000100", "12345678901", "123456789012", "120000", "OWNER", "STORE");
        var line2 = BuildLine("2", "20190301", "0000000200", "12345678901", "123456789012", "120000", "OWNER", "STORE");
        var line3 = BuildLine("3", "20190301", "0000000300", "12345678901", "123456789012", "120000", "OWNER", "STORE");

        // Act
        var result1 = _parser.Parse(line1, 1);
        var result2 = _parser.Parse(line2, 2);
        var result3 = _parser.Parse(line3, 3);

        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.True(result3.IsSuccess);
        
        var record1 = (CnabRecord)result1;
        var record2 = (CnabRecord)result2;
        var record3 = (CnabRecord)result3;
        
        Assert.Equal(1, record1.LineNumber);
        Assert.Equal(2, record2.LineNumber);
        Assert.Equal(3, record3.LineNumber);
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
        var storePadded = store.PadRight(18, ' ');
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

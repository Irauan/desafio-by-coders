using DesafioByCoders.Api.Messages;
using DesafioByCoders.Api.Transactions;

namespace DesafioByCoders.Api.Tests.Units.Transactions;

public class CnabRecordTests
{
    [Fact]
    public void CnabRecord_Create_WithValidLine_ReturnsSuccessAndParsesFields()
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

        // Act
        var result = CnabRecord.Create(line);

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
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CnabRecord_Create_WithEmptyOrWhitespaceLine_ReturnsFailure(string? line)
    {
        // Act
        var result = CnabRecord.Create(line ?? string.Empty);

        // Assert
        Assert.True(result.IsFailure);
        var errors = (List<ValidationError>)result;
        Assert.Contains(errors, e => e.Code == "CNAB_EMPTY_LINE");
    }

    [Fact]
    public void CnabRecord_Create_WithShortLine_ReturnsInvalidLengthFailure()
    {
        // Arrange
        var line = new string('0', 80);

        // Act
        var result = CnabRecord.Create(line);

        // Assert
        Assert.True(result.IsFailure);
        var errors = (List<ValidationError>)result;
        Assert.Contains(errors, e => e.Code == "CNAB_INVALID_LENGTH");
    }

    [Fact]
    public void CnabRecord_Create_WithNonNumericType_ReturnsInvalidTypeFailure()
    {
        // Arrange
        var line = BuildLine("X", "20190301", "0000000001", "12345678901", "123456789012", "000000", "OWNER", "STORE");

        // Act
        var result = CnabRecord.Create(line);

        // Assert
        Assert.True(result.IsFailure);
        var errors = (List<ValidationError>)result;
        Assert.Contains(errors, e => e.Code == "CNAB_INVALID_TYPE");
    }

    [Fact]
    public void CnabRecord_Create_WithUnknownType_ReturnsUnknownTypeFailure()
    {
        // Arrange (type 0 is Unknown per enum, but considered invalid for input)
        var line = BuildLine("0", "20190301", "0000000001", "12345678901", "123456789012", "000000", "OWNER", "STORE");

        // Act
        var result = CnabRecord.Create(line);

        // Assert
        Assert.True(result.IsFailure);
        
        var errors = (List<ValidationError>)result;
        
        Assert.Contains(errors, e => e.Code == "CNAB_UNKNOWN_TYPE");
    }

    [Fact]
    public void CnabRecord_Create_WithInvalidDate_ReturnsInvalidDateFailure()
    {
        // Arrange
        var line = BuildLine("1", "20190230", "0000000001", "12345678901", "123456789012", "000000", "OWNER", "STORE");

        // Act
        var result = CnabRecord.Create(line);

        // Assert
        Assert.True(result.IsFailure);
        var errors = (List<ValidationError>)result;
        Assert.Contains(errors, e => e.Code == "CNAB_INVALID_DATE");
    }

    [Fact]
    public void CnabRecord_Create_WithInvalidTime_ReturnsInvalidTimeFailure()
    {
        // Arrange
        var line = BuildLine("1", "20190301", "0000000001", "12345678901", "123456789012", "246199", "OWNER", "STORE");

        // Act
        var result = CnabRecord.Create(line);

        // Assert
        Assert.True(result.IsFailure);
        var errors = (List<ValidationError>)result;
        Assert.Contains(errors, e => e.Code == "CNAB_INVALID_TIME");
    }

    [Fact]
    public void CnabRecord_Create_WithNonNumericAmount_ReturnsInvalidAmountFailure()
    {
        // Arrange
        var line = BuildLine("1", "20190301", "00000ABCDE", "12345678901", "123456789012", "000000", "OWNER", "STORE");

        // Act
        var result = CnabRecord.Create(line);

        // Assert
        Assert.True(result.IsFailure);
        var errors = (List<ValidationError>)result;
        Assert.Contains(errors, e => e.Code == "CNAB_INVALID_AMOUNT");
    }

    [Fact]
    public void CnabRecord_Create_WithNegativeAmount_ReturnsNegativeAmountFailure()
    {
        // Arrange
        var line = BuildLine("1", "20190301", "-000000010", "12345678901", "123456789012", "000000", "OWNER", "STORE");

        // Act
        var result = CnabRecord.Create(line);

        // Assert
        Assert.True(result.IsFailure);
        var errors = (List<ValidationError>)result;
        Assert.Contains(errors, e => e.Code == "CNAB_NEGATIVE_AMOUNT");
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

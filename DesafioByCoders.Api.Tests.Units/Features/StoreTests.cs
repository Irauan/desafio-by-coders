using DesafioByCoders.Api.Features;

namespace DesafioByCoders.Api.Tests.Units.Features;

public class StoreTests
{
    [Fact]
    public void Store_Create_TrimsNameAndOwner()
    {
        var store = Store.Create("  My Store  ", "  John Doe  ");

        Assert.Equal("My Store", store.Name);
        Assert.Equal("John Doe", store.Owner);
    }

    [Fact]
    public void Store_ToString_ReturnsLowercaseNameAndOwnerSeparatedByDash()
    {
        var store = Store.Create("My STORE", "John DOE");

        Assert.Equal("my store - john doe", store.ToString());
    }

    [Fact]
    public void Store_ToString_LowercasesAccentedAndUnicodeCharacters()
    {
        var store = Store.Create("PADARIA DO ZÉ", "JOSÉ DA SILVA");

        Assert.Equal("padaria do zé - josé da silva", store.ToString());
    }

    [Fact]
    public void Store_Create_WithEmptyStrings_ProducesEmptyPropertiesAndToStringWithDash()
    {
        var store = Store.Create("", "");

        Assert.Equal("", store.Name);
        Assert.Equal("", store.Owner);
        Assert.Equal(" - ", store.ToString());
    }
}

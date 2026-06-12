using CurrencyConverter.Api.Functions;
using Xunit;

namespace CurrencyConverter.Tests.Functions;

public sealed class CurrencyConverterFunctionTests
{
    [Fact]
    public void Parse_ReturnsRequest_ForValidQuery()
    {
        var result = CurrencyConverterRequestParser.Parse("?sourceCurrency=USD&targetCurrency=INR&amount=100.50");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("USD", result.Value!.SourceCurrency);
        Assert.Equal("INR", result.Value.TargetCurrency);
        Assert.Equal(100.50m, result.Value.Amount);
    }

    [Fact]
    public void Parse_ReturnsFailure_WhenAmountIsMissing()
    {
        var result = CurrencyConverterRequestParser.Parse("?sourceCurrency=USD&targetCurrency=INR");

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("INVALID_REQUEST", result.Error!.Code);
    }

    [Fact]
    public void Parse_ReturnsFailure_WhenAmountIsInvalid()
    {
        var result = CurrencyConverterRequestParser.Parse("?sourceCurrency=USD&targetCurrency=INR&amount=abc");

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("INVALID_AMOUNT", result.Error!.Code);
    }
}

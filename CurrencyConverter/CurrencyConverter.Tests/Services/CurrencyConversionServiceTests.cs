using CurrencyConverter.Api.Common;
using CurrencyConverter.Api.Models;
using CurrencyConverter.Api.Services;
using CurrencyConverter.Api.Services.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CurrencyConverter.Tests.Services;

public sealed class CurrencyConversionServiceTests
{
    private sealed class FakeExchangeRateRepository : IExchangeRateRepository
    {
        private readonly Func<string, string, Result<decimal>> _lookup;

        public FakeExchangeRateRepository(Func<string, string, Result<decimal>> lookup)
        {
            _lookup = lookup;
        }

        public Task<Result<decimal>> GetExchangeRateAsync(string sourceCurrency, string targetCurrency, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_lookup(sourceCurrency, targetCurrency));
        }
    }

    [Fact]
    public async Task ConvertAsync_ReturnsConvertedAmount_ForValidPair()
    {
        var repository = new FakeExchangeRateRepository((source, target) =>
        {
            if (source.Equals("USD", StringComparison.OrdinalIgnoreCase) &&
                target.Equals("INR", StringComparison.OrdinalIgnoreCase))
            {
                return Result<decimal>.Success(74m);
            }

            return Result<decimal>.Failure(new ApiError(ErrorCodes.UnsupportedCurrencyPair, "Pair not configured."));
        });

        var service = new CurrencyConversionService(repository, NullLogger<CurrencyConversionService>.Instance);

        var result = await service.ConvertAsync(new ConversionRequest("USD", "INR", 100m));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(74m, result.Value!.ExchangeRate);
        Assert.Equal(7400m, result.Value.ConvertedAmount);
    }

    [Fact]
    public async Task ConvertAsync_ReturnsFailure_ForNegativeAmount()
    {
        var repository = new FakeExchangeRateRepository((_, _) => Result<decimal>.Success(74m));
        var service = new CurrencyConversionService(repository, NullLogger<CurrencyConversionService>.Instance);

        var result = await service.ConvertAsync(new ConversionRequest("USD", "INR", -1m));

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal(ErrorCodes.InvalidAmount, result.Error!.Code);
    }

    [Fact]
    public async Task ConvertAsync_ReturnsFailure_ForUnsupportedCurrency()
    {
        var repository = new FakeExchangeRateRepository((_, _) => Result<decimal>.Success(74m));
        var service = new CurrencyConversionService(repository, NullLogger<CurrencyConversionService>.Instance);

        var result = await service.ConvertAsync(new ConversionRequest("ABC", "INR", 10m));

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal(ErrorCodes.UnsupportedCurrency, result.Error!.Code);
    }

    [Fact]
    public async Task ConvertAsync_PropagatesRepositoryFailure()
    {
        var repository = new FakeExchangeRateRepository((_, _) =>
            Result<decimal>.Failure(new ApiError(ErrorCodes.UnsupportedCurrencyPair, "Pair not configured.")));

        var service = new CurrencyConversionService(repository, NullLogger<CurrencyConversionService>.Instance);

        var result = await service.ConvertAsync(new ConversionRequest("USD", "INR", 10m));

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal(ErrorCodes.UnsupportedCurrencyPair, result.Error!.Code);
    }
}

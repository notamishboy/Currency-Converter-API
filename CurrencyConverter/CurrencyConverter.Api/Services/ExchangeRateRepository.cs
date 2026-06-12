using System.Globalization;
using CurrencyConverter.Api.Common;
using CurrencyConverter.Api.Configuration;
using CurrencyConverter.Api.Services.Interfaces;
using CurrencyConverter.Api.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CurrencyConverter.Api.Services;

public sealed class ExchangeRateRepository : IExchangeRateRepository
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ExchangeRateRepository> _logger;

    public ExchangeRateRepository(IConfiguration configuration, ILogger<ExchangeRateRepository> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task<Result<decimal>> GetExchangeRateAsync(string sourceCurrency, string targetCurrency, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!ExchangeRateConfiguration.IsSupportedCurrency(sourceCurrency))
        {
            return Task.FromResult(Result<decimal>.Failure(
                new ApiError(
                    ErrorCodes.UnsupportedCurrency,
                    $"Unsupported source currency '{sourceCurrency}'. Supported currencies are USD, INR and EUR.")));
        }

        if (!ExchangeRateConfiguration.IsSupportedCurrency(targetCurrency))
        {
            return Task.FromResult(Result<decimal>.Failure(
                new ApiError(
                    ErrorCodes.UnsupportedCurrency,
                    $"Unsupported target currency '{targetCurrency}'. Supported currencies are USD, INR and EUR.")));
        }

        var normalizedSource = ExchangeRateConfiguration.NormalizeCurrencyCode(sourceCurrency);
        var normalizedTarget = ExchangeRateConfiguration.NormalizeCurrencyCode(targetCurrency);

        if (normalizedSource == normalizedTarget)
        {
            return Task.FromResult(Result<decimal>.Success(1m));
        }

        var rateKey = ExchangeRateConfiguration.BuildRateKey(normalizedSource, normalizedTarget);
        var rawRate = _configuration[rateKey];

        if (string.IsNullOrWhiteSpace(rawRate))
        {
            _logger.LogWarning("Exchange rate key {RateKey} is missing.", rateKey);
            return Task.FromResult(Result<decimal>.Failure(
                new ApiError(
                    ErrorCodes.UnsupportedCurrencyPair,
                    $"Exchange rate for the pair '{normalizedSource}' to '{normalizedTarget}' is not configured.")));
        }

        if (!decimal.TryParse(rawRate, NumberStyles.Number, CultureInfo.InvariantCulture, out var exchangeRate) || exchangeRate <= 0m)
        {
            _logger.LogWarning("Exchange rate key {RateKey} contains an invalid value: {RawRate}", rateKey, rawRate);
            return Task.FromResult(Result<decimal>.Failure(
                new ApiError(
                    ErrorCodes.UnsupportedCurrencyPair,
                    $"Exchange rate for the pair '{normalizedSource}' to '{normalizedTarget}' is invalid.")));
        }

        return Task.FromResult(Result<decimal>.Success(exchangeRate));
    }
}

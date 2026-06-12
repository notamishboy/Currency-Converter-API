using CurrencyConverter.Api.Common;
using CurrencyConverter.Api.Configuration;
using CurrencyConverter.Api.Models;
using CurrencyConverter.Api.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace CurrencyConverter.Api.Services;

public sealed class CurrencyConversionService : ICurrencyConversionService
{
    private readonly IExchangeRateRepository _exchangeRateRepository;
    private readonly ILogger<CurrencyConversionService> _logger;

    public CurrencyConversionService(IExchangeRateRepository exchangeRateRepository, ILogger<CurrencyConversionService> logger)
    {
        _exchangeRateRepository = exchangeRateRepository;
        _logger = logger;
    }

    public async Task<Result<ConversionResponse>> ConvertAsync(ConversionRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return Result<ConversionResponse>.Failure(
                new ApiError(
                    ErrorCodes.InvalidRequest,
                    "The conversion request cannot be null."));
        }

        if (!ExchangeRateConfiguration.IsSupportedCurrency(request.SourceCurrency))
        {
            return Result<ConversionResponse>.Failure(
                new ApiError(
                    ErrorCodes.UnsupportedCurrency,
                    $"Unsupported source currency '{request.SourceCurrency}'. Supported currencies are USD, INR and EUR."));
        }

        if (!ExchangeRateConfiguration.IsSupportedCurrency(request.TargetCurrency))
        {
            return Result<ConversionResponse>.Failure(
                new ApiError(
                    ErrorCodes.UnsupportedCurrency,
                    $"Unsupported target currency '{request.TargetCurrency}'. Supported currencies are USD, INR and EUR."));
        }

        if (request.Amount < 0m)
        {
            return Result<ConversionResponse>.Failure(
                new ApiError(
                    ErrorCodes.InvalidAmount,
                    "The amount must be greater than or equal to zero."));
        }

        var exchangeRateResult = await _exchangeRateRepository.GetExchangeRateAsync(
            request.SourceCurrency,
            request.TargetCurrency,
            cancellationToken);

        if (!exchangeRateResult.IsSuccess || exchangeRateResult.Value is null)
        {
            return Result<ConversionResponse>.Failure(
                exchangeRateResult.Error ?? new ApiError(
                    ErrorCodes.UnsupportedCurrencyPair,
                    "Unable to resolve the exchange rate for the requested currency pair."));
        }

        var exchangeRate = exchangeRateResult.Value.Value;
        var convertedAmount = Math.Round(request.Amount * exchangeRate, 4, MidpointRounding.AwayFromZero);

        _logger.LogInformation(
            "Converted {Amount} {SourceCurrency} to {ConvertedAmount} {TargetCurrency} using rate {ExchangeRate}.",
            request.Amount,
            ExchangeRateConfiguration.NormalizeCurrencyCode(request.SourceCurrency),
            convertedAmount,
            ExchangeRateConfiguration.NormalizeCurrencyCode(request.TargetCurrency),
            exchangeRate);

        return Result<ConversionResponse>.Success(new ConversionResponse(exchangeRate, convertedAmount));
    }
}

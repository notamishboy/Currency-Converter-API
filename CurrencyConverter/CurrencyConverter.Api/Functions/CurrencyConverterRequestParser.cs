using System.Globalization;
using System.Net;
using CurrencyConverter.Api.Common;
using CurrencyConverter.Api.Models;

namespace CurrencyConverter.Api.Functions;

public static class CurrencyConverterRequestParser
{
    public static Result<ConversionRequest> Parse(string? queryString)
    {
        var query = ParseQuery(queryString);

        if (!query.TryGetValue("sourceCurrency", out var sourceCurrency) || string.IsNullOrWhiteSpace(sourceCurrency))
        {
            return Result<ConversionRequest>.Failure(
                new ApiError(
                    ErrorCodes.InvalidRequest,
                    "Missing required query parameter 'sourceCurrency'."));
        }

        if (!query.TryGetValue("targetCurrency", out var targetCurrency) || string.IsNullOrWhiteSpace(targetCurrency))
        {
            return Result<ConversionRequest>.Failure(
                new ApiError(
                    ErrorCodes.InvalidRequest,
                    "Missing required query parameter 'targetCurrency'."));
        }

        if (!query.TryGetValue("amount", out var amountValue) || string.IsNullOrWhiteSpace(amountValue))
        {
            return Result<ConversionRequest>.Failure(
                new ApiError(
                    ErrorCodes.InvalidRequest,
                    "Missing required query parameter 'amount'."));
        }

        if (!decimal.TryParse(amountValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
        {
            return Result<ConversionRequest>.Failure(
                new ApiError(
                    ErrorCodes.InvalidAmount,
                    $"The amount '{amountValue}' is not a valid decimal number."));
        }

        if (amount < 0m)
        {
            return Result<ConversionRequest>.Failure(
                new ApiError(
                    ErrorCodes.InvalidAmount,
                    "The amount must be greater than or equal to zero."));
        }

        return Result<ConversionRequest>.Success(
            new ConversionRequest(
                sourceCurrency.Trim(),
                targetCurrency.Trim(),
                amount));
    }

    internal static Dictionary<string, string> ParseQuery(string? queryString)
    {
        var query = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(queryString))
        {
            return query;
        }

        var trimmedQuery = queryString.Trim().TrimStart('?');
        if (string.IsNullOrWhiteSpace(trimmedQuery))
        {
            return query;
        }

        foreach (var pair in trimmedQuery.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = pair.Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 0 || string.IsNullOrWhiteSpace(parts[0]))
            {
                continue;
            }

            var key = WebUtility.UrlDecode(parts[0]) ?? string.Empty;
            var value = parts.Length > 1 ? WebUtility.UrlDecode(parts[1]) ?? string.Empty : string.Empty;
            query[key] = value;
        }

        return query;
    }
}

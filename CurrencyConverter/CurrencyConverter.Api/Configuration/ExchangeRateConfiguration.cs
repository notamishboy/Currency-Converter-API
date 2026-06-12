using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace CurrencyConverter.Api.Configuration;

public static class ExchangeRateConfiguration
{
    private static readonly HashSet<string> SupportedCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "USD",
        "INR",
        "EUR"
    };

    private static readonly string[] RequiredRateKeys =
    {
        "USD_TO_INR",
        "INR_TO_USD",
        "USD_TO_EUR",
        "EUR_TO_USD",
        "INR_TO_EUR",
        "EUR_TO_INR"
    };

    public static bool IsValidCurrencyCode(string? currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            return false;
        }

        var trimmed = currencyCode.Trim();
        return trimmed.Length == 3 && trimmed.All(char.IsLetter);
    }

    public static bool IsSupportedCurrency(string? currencyCode)
    {
        return IsValidCurrencyCode(currencyCode) && SupportedCurrencies.Contains(NormalizeCurrencyCode(currencyCode!));
    }

    public static string NormalizeCurrencyCode(string currencyCode)
    {
        return currencyCode.Trim().ToUpperInvariant();
    }

    public static string BuildRateKey(string sourceCurrency, string targetCurrency)
    {
        return $"{NormalizeCurrencyCode(sourceCurrency)}_TO_{NormalizeCurrencyCode(targetCurrency)}";
    }

    public static void ValidateBootstrapConfiguration(IConfiguration configuration)
    {
        var missingKeys = new List<string>();
        var invalidKeys = new List<string>();

        foreach (var key in RequiredRateKeys)
        {
            var rawValue = configuration[key];

            if (string.IsNullOrWhiteSpace(rawValue))
            {
                missingKeys.Add(key);
                continue;
            }

            if (!decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedRate) || parsedRate <= 0m)
            {
                invalidKeys.Add(key);
            }
        }

        if (missingKeys.Count == 0 && invalidKeys.Count == 0)
        {
            return;
        }

        var messageParts = new List<string>();

        if (missingKeys.Count > 0)
        {
            messageParts.Add($"Missing exchange rates: {string.Join(", ", missingKeys)}.");
        }

        if (invalidKeys.Count > 0)
        {
            messageParts.Add($"Invalid exchange rate values: {string.Join(", ", invalidKeys)}.");
        }

        throw new InvalidOperationException(string.Join(" ", messageParts));
    }
}

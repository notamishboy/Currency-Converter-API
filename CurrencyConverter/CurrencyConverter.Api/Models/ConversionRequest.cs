namespace CurrencyConverter.Api.Models;

public sealed record ConversionRequest(string SourceCurrency, string TargetCurrency, decimal Amount);

namespace CurrencyConverter.Api.Models;

public sealed record ConversionResponse(decimal ExchangeRate, decimal ConvertedAmount);

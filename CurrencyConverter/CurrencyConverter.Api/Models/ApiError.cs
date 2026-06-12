namespace CurrencyConverter.Api.Models;

public sealed record ApiError(string Code, string Message, string? Details = null);

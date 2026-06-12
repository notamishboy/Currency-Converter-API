using CurrencyConverter.Api.Common;

namespace CurrencyConverter.Api.Services.Interfaces;

public interface IExchangeRateRepository
{
    Task<Result<decimal>> GetExchangeRateAsync(string sourceCurrency, string targetCurrency, CancellationToken cancellationToken = default);
}

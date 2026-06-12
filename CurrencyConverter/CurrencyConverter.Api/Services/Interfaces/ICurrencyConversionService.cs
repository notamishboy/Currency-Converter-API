using CurrencyConverter.Api.Common;
using CurrencyConverter.Api.Models;

namespace CurrencyConverter.Api.Services.Interfaces;

public interface ICurrencyConversionService
{
    Task<Result<ConversionResponse>> ConvertAsync(ConversionRequest request, CancellationToken cancellationToken = default);
}

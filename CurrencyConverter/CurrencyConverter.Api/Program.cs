using CurrencyConverter.Api.Common;
using CurrencyConverter.Api.Configuration;
using CurrencyConverter.Api.Services;
using CurrencyConverter.Api.Services.Interfaces;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile(
    Path.Combine(AppContext.BaseDirectory, "exchangeRates.json"),
    optional: false,
    reloadOnChange: true);

builder.Configuration.AddEnvironmentVariables();

ExchangeRateConfiguration.ValidateBootstrapConfiguration(builder.Configuration);

builder.Services.AddSingleton<IExchangeRateRepository, ExchangeRateRepository>();
builder.Services.AddSingleton<ICurrencyConversionService, CurrencyConversionService>();

builder.Logging.AddConsole();

await builder.Build().RunAsync();

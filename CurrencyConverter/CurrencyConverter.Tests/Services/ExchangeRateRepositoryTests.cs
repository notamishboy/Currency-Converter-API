using System.Text.Json;
using CurrencyConverter.Api.Common;
using CurrencyConverter.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CurrencyConverter.Tests.Services;

public sealed class ExchangeRateRepositoryTests
{
    [Fact]
    public async Task GetExchangeRateAsync_ReturnsRate_FromJsonFile()
    {
        using var tempDirectory = new TempDirectory();
        var filePath = tempDirectory.GetPath("exchangeRates.json");
        WriteRates(filePath, new Dictionary<string, decimal>
        {
            ["USD_TO_INR"] = 74.00m,
            ["INR_TO_USD"] = 0.013m,
            ["USD_TO_EUR"] = 0.85m,
            ["EUR_TO_USD"] = 1.18m,
            ["INR_TO_EUR"] = 0.011m,
            ["EUR_TO_INR"] = 88.00m
        });

        var config = BuildConfiguration(filePath);
        var repository = new ExchangeRateRepository(config, NullLogger<ExchangeRateRepository>.Instance);

        var result = await repository.GetExchangeRateAsync("USD", "INR");

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.HasValue);
        Assert.Equal(74.00m, result.Value.Value);
    }

    [Fact]
    public async Task GetExchangeRateAsync_UsesEnvironmentVariableOverride()
    {
        using var tempDirectory = new TempDirectory();
        var filePath = tempDirectory.GetPath("exchangeRates.json");
        WriteRates(filePath, new Dictionary<string, decimal>
        {
            ["USD_TO_INR"] = 74.00m,
            ["INR_TO_USD"] = 0.013m,
            ["USD_TO_EUR"] = 0.85m,
            ["EUR_TO_USD"] = 1.18m,
            ["INR_TO_EUR"] = 0.011m,
            ["EUR_TO_INR"] = 88.00m
        });

        const string envKey = "USD_TO_INR";
        var originalValue = Environment.GetEnvironmentVariable(envKey);

        try
        {
            Environment.SetEnvironmentVariable(envKey, "81.00");

            var config = BuildConfiguration(filePath);
            var repository = new ExchangeRateRepository(config, NullLogger<ExchangeRateRepository>.Instance);

            var result = await repository.GetExchangeRateAsync("USD", "INR");

            Assert.True(result.IsSuccess);
            Assert.True(result.Value.HasValue);
            Assert.Equal(81.00m, result.Value.Value);
        }
        finally
        {
            Environment.SetEnvironmentVariable(envKey, originalValue);
        }
    }

    [Fact]
    public async Task GetExchangeRateAsync_PicksUpFileChanges_WithoutRestart()
    {
        using var tempDirectory = new TempDirectory();
        var filePath = tempDirectory.GetPath("exchangeRates.json");
        WriteRates(filePath, new Dictionary<string, decimal>
        {
            ["USD_TO_INR"] = 74.00m,
            ["INR_TO_USD"] = 0.013m,
            ["USD_TO_EUR"] = 0.85m,
            ["EUR_TO_USD"] = 1.18m,
            ["INR_TO_EUR"] = 0.011m,
            ["EUR_TO_INR"] = 88.00m
        });

        var config = BuildConfiguration(filePath);
        var repository = new ExchangeRateRepository(config, NullLogger<ExchangeRateRepository>.Instance);

        var initial = await repository.GetExchangeRateAsync("USD", "INR");
        Assert.True(initial.IsSuccess);
        Assert.True(initial.Value.HasValue);
        Assert.Equal(74.00m, initial.Value.Value);

        WriteRates(filePath, new Dictionary<string, decimal>
        {
            ["USD_TO_INR"] = 79.00m,
            ["INR_TO_USD"] = 0.013m,
            ["USD_TO_EUR"] = 0.85m,
            ["EUR_TO_USD"] = 1.18m,
            ["INR_TO_EUR"] = 0.011m,
            ["EUR_TO_INR"] = 88.00m
        });

        var success = await WaitForAsync(async () =>
        {
            var result = await repository.GetExchangeRateAsync("USD", "INR");
            return result.IsSuccess && result.Value.HasValue && result.Value.Value == 79.00m;
        }, TimeSpan.FromSeconds(5));

        Assert.True(success);
    }

    [Fact]
    public async Task GetExchangeRateAsync_ReturnsFailure_ForUnsupportedPair()
    {
        using var tempDirectory = new TempDirectory();
        var filePath = tempDirectory.GetPath("exchangeRates.json");
        WriteRates(filePath, new Dictionary<string, decimal>
        {
            ["USD_TO_INR"] = 74.00m,
            ["INR_TO_USD"] = 0.013m
        });

        var config = BuildConfiguration(filePath);
        var repository = new ExchangeRateRepository(config, NullLogger<ExchangeRateRepository>.Instance);

        var result = await repository.GetExchangeRateAsync("USD", "EUR");

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal(ErrorCodes.UnsupportedCurrencyPair, result.Error!.Code);
    }

    private static IConfigurationRoot BuildConfiguration(string filePath)
    {
        return new ConfigurationBuilder()
            .AddJsonFile(filePath, optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
    }

    private static void WriteRates(string filePath, IDictionary<string, decimal> rates)
    {
        var json = JsonSerializer.Serialize(rates, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(filePath, json);
    }

    private static async Task<bool> WaitForAsync(Func<Task<bool>> condition, TimeSpan timeout)
    {
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < timeout)
        {
            if (await condition())
            {
                return true;
            }

            await Task.Delay(150);
        }

        return false;
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            DirectoryPath = Directory.CreateDirectory(System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"))).FullName;
        }

        public string DirectoryPath { get; }

        public string GetPath(string fileName) => System.IO.Path.Combine(DirectoryPath, fileName);

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(DirectoryPath))
                {
                    Directory.Delete(DirectoryPath, recursive: true);
                }
            }
            catch
            {
                // Best-effort cleanup for test artifacts.
            }
        }
    }
}

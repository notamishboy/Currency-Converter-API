# Currency Converter API

Azure Functions isolated worker API on .NET 10.

## What it does

`GET /convert?sourceCurrency=USD&targetCurrency=INR&amount=100`

Response:
```json
{
  "exchangeRate": 74,
  "convertedAmount": 7400
}
```

The implementation uses:

- Azure Functions v4
- .NET 10 isolated worker
- local `exchangeRates.json` file as the primary source of rates
- environment variables as overrides
- file reload on change for the bonus requirement
- unit tests for conversion logic, repository behavior, and request parsing

## Project structure

```text
CurrencyConverter/
├── CurrencyConverter.Api/
│   ├── Functions/
│   ├── Models/
│   ├── Services/
│   ├── Configuration/
│   ├── Common/
│   ├── exchangeRates.json
│   ├── local.settings.json
│   ├── host.json
│   └── Program.cs
├── CurrencyConverter.Tests/
└── CurrencyConverter.sln
```

## Prerequisites

Install:

- .NET 10 SDK
- Azure Functions Core Tools v4
- Azurite, or use an equivalent local storage emulator

## Run locally

1. Open a terminal in `CurrencyConverter/CurrencyConverter.Api`
2. Restore packages:
   ```bash
   dotnet restore
   ```
3. Start the function host:
   ```bash
   func start
   ```

If you are using Visual Studio or VS Code, you can also run the function app from the IDE after restoring packages.

## Test the API

```bash
curl "http://localhost:7071/convert?sourceCurrency=USD&targetCurrency=INR&amount=100"
```

## Override exchange rates

`exchangeRates.json` contains the default rates.

To override a rate locally, add an environment variable under `Values` in `local.settings.json`, for example:

```json
"USD_TO_INR": "81.00"
```

The environment variable value wins over the JSON file value.

## Dynamic configuration

The app loads `exchangeRates.json` with reload-on-change enabled. Update the file while the host is running and the new rate is picked up without restarting the application.

## Notes on error handling

The API returns clear `400` responses for:

- missing query parameters
- invalid currency codes
- negative amounts
- unsupported currency pairs

## Run tests

From the solution root:

```bash
dotnet test
```

using System.Net;
using CurrencyConverter.Api.Common;
using CurrencyConverter.Api.Models;
using CurrencyConverter.Api.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CurrencyConverter.Api.Functions;

public sealed class CurrencyConverterFunction
{
    private readonly ICurrencyConversionService _conversionService;
    private readonly ILogger<CurrencyConverterFunction> _logger;

    public CurrencyConverterFunction(ICurrencyConversionService conversionService, ILogger<CurrencyConverterFunction> logger)
    {
        _conversionService = conversionService;
        _logger = logger;
    }

    [Function("ConvertCurrency")]
    public async Task<HttpResponseData> ConvertAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "convert")] HttpRequestData request)
    {
        _logger.LogInformation("Received currency conversion request: {Url}", request.Url);

        var parsedRequest = CurrencyConverterRequestParser.Parse(request.Url.Query);
        if (!parsedRequest.IsSuccess)
        {
            _logger.LogWarning("Request validation failed: {ErrorCode} - {Message}", parsedRequest.Error!.Code, parsedRequest.Error.Message);
            return await CreateErrorResponseAsync(request, HttpStatusCode.BadRequest, parsedRequest.Error);
        }

        var conversionResult = await _conversionService.ConvertAsync(parsedRequest.Value!);
        if (!conversionResult.IsSuccess)
        {
            _logger.LogWarning("Conversion failed: {ErrorCode} - {Message}", conversionResult.Error!.Code, conversionResult.Error.Message);
            return await CreateErrorResponseAsync(request, HttpStatusCode.BadRequest, conversionResult.Error);
        }

        var response = request.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(conversionResult.Value);
        _logger.LogInformation(
            "Conversion succeeded for {SourceCurrency} to {TargetCurrency}.",
            parsedRequest.Value!.SourceCurrency,
            parsedRequest.Value.TargetCurrency);

        return response;
    }

    private static async Task<HttpResponseData> CreateErrorResponseAsync(
        HttpRequestData request,
        HttpStatusCode statusCode,
        ApiError error)
    {
        var response = request.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(error);
        return response;
    }
}

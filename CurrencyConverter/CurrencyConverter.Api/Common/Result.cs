using CurrencyConverter.Api.Models;

namespace CurrencyConverter.Api.Common;

public sealed record Result<T>(bool IsSuccess, T? Value, ApiError? Error)
{
    public static Result<T> Success(T value) => new(true, value, null);

    public static Result<T> Failure(ApiError error) => new(false, default, error);
}

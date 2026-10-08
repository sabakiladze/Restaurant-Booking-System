namespace RestaurantBooking.Application.Common;

public record Result(bool Success, string? Error)
{
    public static Result Ok() => new(true, null);
    public static Result Fail(string error) => new(false, error);
}

public record Result<T>(bool Success, string? Error, T? Value)
{
    public static Result<T> Ok(T value) => new(true, null, value);
    public static Result<T> Fail(string error) => new(false, error, default);
}

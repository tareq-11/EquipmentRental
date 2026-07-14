namespace Core.Common;

/// <summary>Represents an expected application outcome without using exceptions for flow control.</summary>
public class Result
{
    protected Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>Gets whether the operation succeeded.</summary>
    public bool IsSuccess { get; }
    /// <summary>Gets the expected error when the operation failed.</summary>
    public Error? Error { get; }
    /// <summary>Creates a successful result.</summary>
    public static Result Success() => new(true, null);
    /// <summary>Creates a failed result.</summary>
    public static Result Failure(Error error) => new(false, error);
}

/// <summary>Represents a successful or expected failed result with a value.</summary>
public sealed class Result<T>(bool isSuccess, T? value, Error? error) : Result(isSuccess, error)
{
    /// <summary>Gets the success value.</summary>
    public T? Value { get; } = value;
    /// <summary>Creates a successful result.</summary>
    public static Result<T> Success(T value) => new(true, value, null);
    /// <summary>Creates a failed result.</summary>
    public new static Result<T> Failure(Error error) => new(false, default, error);
}

/// <summary>Describes an expected, client-safe error.</summary>
public sealed record Error(string Code, string Message, ErrorType Type, IReadOnlyDictionary<string, string[]>? Fields = null)
{
    /// <summary>Creates a validation error.</summary>
    public static Error Validation(IReadOnlyDictionary<string, string[]> fields) => new("validation_failed", "Correct the highlighted fields.", ErrorType.Validation, fields);
}

/// <summary>Classifies errors for transport mapping.</summary>
public enum ErrorType { Validation, Unauthorized, Forbidden, NotFound, Conflict, RateLimited, Unexpected }

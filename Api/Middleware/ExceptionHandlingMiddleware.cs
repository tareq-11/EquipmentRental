using Core.Common;
using Shared;
using Microsoft.AspNetCore.Mvc;

namespace Api.Middleware;

/// <summary>Maps unexpected exceptions to a safe standard envelope.</summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    /// <summary>Handles unanticipated errors after the request pipeline has run.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (ApiException exception)
        {
            logger.LogWarning("Expected API error {ErrorCode}", exception.Error.Code);
            await WriteAsync(context, exception.Error);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled API exception");
            await WriteAsync(context, new Error("unexpected_error", "An unexpected error occurred.", ErrorType.Unexpected));
        }
    }

    /// <summary>Writes a standard error response.</summary>
    public static async Task WriteAsync(HttpContext context, Error error)
    {
        var code = ApiResponseMapper.StatusCode(error);
        context.Response.StatusCode = code;
        await context.Response.WriteAsJsonAsync(new ApiResponse<ApiErrorData>(code, error.Message, new ApiErrorData(error.Fields)));
    }
}

/// <summary>Centralizes Result and exception error-to-envelope translation.</summary>
public static class ApiResponseMapper
{
    /// <summary>Returns the HTTP status for a client-safe error.</summary>
    public static int StatusCode(Error error) => error.Type switch { ErrorType.Validation => StatusCodes.Status400BadRequest, ErrorType.Unauthorized => StatusCodes.Status401Unauthorized, ErrorType.Forbidden => StatusCodes.Status403Forbidden, ErrorType.NotFound => StatusCodes.Status404NotFound, ErrorType.Conflict => StatusCodes.Status409Conflict, ErrorType.RateLimited => StatusCodes.Status429TooManyRequests, _ => StatusCodes.Status500InternalServerError };

    /// <summary>Maps an application result to the standard API envelope.</summary>
    public static IActionResult ToActionResult<T>(ControllerBase controller, Result<T> result, string successMessage, int successStatus = StatusCodes.Status200OK)
    {
        if (result.IsSuccess) return controller.StatusCode(successStatus, new ApiResponse<T>(successStatus, successMessage, result.Value));
        var error = result.Error!;
        var status = StatusCode(error);
        return controller.StatusCode(status, new ApiResponse<ApiErrorData>(status, error.Message, new ApiErrorData(error.Fields)));
    }
}

/// <summary>Represents an expected error raised at the API boundary.</summary>
public sealed class ApiException(Error error) : Exception(error.Message)
{
    /// <summary>Gets the client-safe error.</summary>
    public Error Error { get; } = error;
}

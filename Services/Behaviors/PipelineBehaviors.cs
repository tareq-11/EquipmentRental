using Core.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Services.Abstractions;
using System.Text.Json;

namespace Services.Behaviors;

/// <summary>Logs and safely rethrows unexpected handler exceptions.</summary>
public sealed class UnhandledExceptionBehavior<TRequest, TResponse>(ILogger<UnhandledExceptionBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try { return await next(cancellationToken); }
        catch (Exception exception) { logger.LogError(exception, "Unhandled request {RequestName}", typeof(TRequest).Name); throw; }
    }
}

/// <summary>Measures command and query duration.</summary>
public sealed class PerformanceBehavior<TRequest, TResponse>(TimeProvider clock, ILogger<PerformanceBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var start = clock.GetTimestamp(); var response = await next(cancellationToken); var elapsed = clock.GetElapsedTime(start);
        logger.LogInformation("Handled {RequestName} in {ElapsedMilliseconds} ms", typeof(TRequest).Name, elapsed.TotalMilliseconds);
        return response;
    }
}

/// <summary>Turns FluentValidation failures into field-specific result errors.</summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var failures = (await Task.WhenAll(validators.Select(x => x.ValidateAsync(request, cancellationToken)))).SelectMany(x => x.Errors).Where(x => x is not null).ToArray();
        if (failures.Length == 0) return await next(cancellationToken);
        var fields = failures.GroupBy(x => x.PropertyName).ToDictionary(x => x.Key, x => x.Select(x => x.ErrorMessage).Distinct().ToArray());
        return (TResponse)Activator.CreateInstance(typeof(TResponse), false, null, Error.Validation(fields))!;
    }
}

/// <summary>Atomically protects every marked command with a durable actor/key/fingerprint record.</summary>
public sealed class IdempotencyBehavior<TRequest, TResponse>(IIdempotencyCoordinator coordinator, IActorContext actor) : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not IIdempotentCommand command) return await next(cancellationToken);

        var fingerprint = CreateFingerprint(request);
        var outcome = await coordinator.ExecuteAsync(command.IdempotencyKey, typeof(TRequest).FullName ?? typeof(TRequest).Name, actor.Scope, fingerprint, () => next(cancellationToken), IsSuccess, cancellationToken);
        if (outcome.Error is not null) return Failure(outcome.Error);
        return outcome.IsReplay ? MarkReplay(outcome.Response!) : outcome.Response!;
    }

    private static string CreateFingerprint(TRequest request)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(request));
        var fields = document.RootElement.EnumerateObject().Where(x => x.Name is not "IdempotencyKey" and not "IpAddress").OrderBy(x => x.Name, StringComparer.Ordinal).Select(x => $"{x.Name}:{x.Value.GetRawText()}");
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(string.Join("|", fields))));
    }

    private static bool IsSuccess(TResponse response) => (bool?)typeof(TResponse).GetProperty(nameof(Result.IsSuccess))?.GetValue(response) == true;
    private static TResponse Failure(Error error) => (TResponse)Activator.CreateInstance(typeof(TResponse), false, null, error)!;
    private static TResponse MarkReplay(TResponse response)
    {
        var value = typeof(TResponse).GetProperty("Value")?.GetValue(response);
        if (value is not IIdempotentReplayResponse replayable) return response;
        return (TResponse)Activator.CreateInstance(typeof(TResponse), true, replayable.AsIdempotentReplay(), null)!;
    }
}

/// <summary>Optional cache hook. It is deliberately a pass-through until a non-authoritative query is introduced.</summary>
public sealed class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) => next(cancellationToken);
}

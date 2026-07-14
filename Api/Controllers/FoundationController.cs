using Api.Middleware;
using Core.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Services.Foundation;
using Shared;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Controllers;

/// <summary>Development-only, non-business endpoints for manually verifying application conventions.</summary>
[ApiController]
[Route("api/foundation")]
public sealed class FoundationController(ISender sender, IWebHostEnvironment environment) : ControllerBase
{
    /// <summary>Exercises command validation, idempotency, domain events, outbox, audit, time, and response mapping.</summary>
    [HttpPost("probes")]
    [EnableRateLimiting("foundation")]
    public async Task<IActionResult> CreateProbe([FromBody] CreateFoundationProbeRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken)
    {
        EnsureDevelopment();
        var result = await sender.Send(new CreateFoundationProbeCommand(request.Label ?? string.Empty, idempotencyKey ?? string.Empty, request.Reason, HttpContext.Connection.RemoteIpAddress?.ToString()), cancellationToken);
        return ApiResponseMapper.ToActionResult(this, result, "Foundation probe recorded.");
    }

    /// <summary>Exercises EF optimistic concurrency through an intentionally non-business foundation record.</summary>
    [HttpPut("probes/{id:guid}")]
    public async Task<IActionResult> UpdateProbe(Guid id, [FromBody] UpdateFoundationProbeRequest request, CancellationToken cancellationToken)
    {
        EnsureDevelopment();
        var result = await sender.Send(new UpdateFoundationProbeCommand(id, request.Label ?? string.Empty, request.Version), cancellationToken);
        return ApiResponseMapper.ToActionResult(this, result, "Foundation probe updated.");
    }

    /// <summary>Returns controlled expected and unexpected failures to verify status mapping without product features.</summary>
    [HttpGet("errors/{kind}")]
    public IActionResult Error(string kind)
    {
        EnsureDevelopment();
        if (string.Equals(kind, "unexpected", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Foundation verification exception.");
        var error = kind.ToLowerInvariant() switch
        {
            "unauthorized" => new Error("unauthorized", "Authentication is required.", ErrorType.Unauthorized),
            "forbidden" => new Error("forbidden", "You are not allowed to perform this action.", ErrorType.Forbidden),
            "not-found" => new Error("not_found", "The requested foundation resource was not found.", ErrorType.NotFound),
            "conflict" => new Error("conflict", "The foundation resource conflicts with the current state.", ErrorType.Conflict, new Dictionary<string, string[]> { ["resource"] = ["Refresh and try again."] }),
            _ => new Error("validation_failed", "Correct the highlighted fields.", ErrorType.Validation, new Dictionary<string, string[]> { ["kind"] = ["Use unauthorized, forbidden, not-found, conflict, or unexpected."] })
        };
        throw new ApiException(error);
    }

    private void EnsureDevelopment()
    {
        if (!environment.IsDevelopment()) throw new ApiException(new Error("not_found", "The requested resource was not found.", ErrorType.NotFound));
    }
}

/// <summary>Input contract for the non-business foundation probe.</summary>
public sealed record CreateFoundationProbeRequest(string? Label, string? Reason);
/// <summary>Input contract for the development-only stale-write probe.</summary>
public sealed record UpdateFoundationProbeRequest(string? Label, uint Version);

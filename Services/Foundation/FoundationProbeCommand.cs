using Core.Common;
using Core.Foundation;
using FluentValidation;
using MediatR;
using Services.Abstractions;

namespace Services.Foundation;

/// <summary>Creates a non-business probe to demonstrate the command pipeline and persistence conventions.</summary>
public sealed record CreateFoundationProbeCommand(string Label, string IdempotencyKey, string? Reason, string? IpAddress) : ICommand<FoundationProbeResponse>, IIdempotentCommand;

/// <summary>Manual response mapping for a foundation probe.</summary>
public sealed record FoundationProbeResponse(Guid Id, string Label, DateTimeOffset CreatedAt, string DisplayTime, uint Version, bool IdempotentReplay) : IIdempotentReplayResponse
{
    /// <inheritdoc />
    public object AsIdempotentReplay() => this with { IdempotentReplay = true };
}

/// <summary>Validates the safe, non-business foundation probe input.</summary>
public sealed class CreateFoundationProbeCommandValidator : AbstractValidator<CreateFoundationProbeCommand>
{
    /// <summary>Initializes validation rules.</summary>
    public CreateFoundationProbeCommandValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(100).Matches("^[a-zA-Z0-9 ._-]+$").WithMessage("Label may contain letters, numbers, spaces, periods, underscores, and hyphens.");
        RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Reason).MaximumLength(1000).When(x => x.Reason is not null);
    }
}

/// <summary>Persists one probe, its outbox event, idempotency record, and audit record in one unit of work.</summary>
public sealed class CreateFoundationProbeCommandHandler(IRepository<FoundationProbe> probes, IUnitOfWork unitOfWork, IFoundationProbeStore store, IActorContext actor, TimeProvider clock) : IRequestHandler<CreateFoundationProbeCommand, Result<FoundationProbeResponse>>
{
    /// <inheritdoc />
    public async Task<Result<FoundationProbeResponse>> Handle(CreateFoundationProbeCommand request, CancellationToken cancellationToken)
    {
        var probe = FoundationProbe.Create(request.Label, clock);
        await probes.AddAsync(probe, cancellationToken);
        store.AddAudit(actor.UserId, actor.ActorType, "foundation.probe.created", nameof(FoundationProbe), probe.Id.ToString(), request.IpAddress, request.Reason, clock.GetUtcNow());
        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (!saved.IsSuccess) return Result<FoundationProbeResponse>.Failure(saved.Error!);
        return Result<FoundationProbeResponse>.Success(Map(probe, false));
    }

    private static FoundationProbeResponse Map(FoundationProbe probe, bool replay) => new(probe.Id, probe.Label, probe.CreatedAt, TimeZoneInfo.ConvertTimeBySystemTimeZoneId(probe.CreatedAt, "Asia/Amman").ToString("yyyy-MM-dd HH:mm zzz"), probe.Version, replay);
}

/// <summary>Hides persistence-specific idempotency and audit details from application handlers.</summary>
public interface IFoundationProbeStore
{
    /// <summary>Loads a probe tracked for the development-only concurrency demonstration.</summary>
    Task<FoundationProbe?> GetAsync(Guid id, CancellationToken cancellationToken);
    /// <summary>Applies the client version as EF's optimistic concurrency original value.</summary>
    void SetExpectedVersion(FoundationProbe probe, uint version);
    /// <summary>Stages an audit record; future manual override handlers must supply a reason.</summary>
    void AddAudit(Guid? actingUserId, string actorType, string action, string targetType, string targetId, string? ipAddress, string? reason, DateTimeOffset occurredAt);
}

/// <summary>Updates a probe only when the caller still holds its observed version.</summary>
public sealed record UpdateFoundationProbeCommand(Guid Id, string Label, uint Version) : ICommand<FoundationProbeResponse>;

/// <summary>Validates a foundation probe update before its handler can load or mutate a record.</summary>
public sealed class UpdateFoundationProbeCommandValidator : AbstractValidator<UpdateFoundationProbeCommand>
{
    /// <summary>Initializes update validation rules.</summary>
    public UpdateFoundationProbeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Label).NotEmpty().MaximumLength(100).Matches("^[a-zA-Z0-9 ._-]+$").WithMessage("Label may contain letters, numbers, spaces, periods, underscores, and hyphens.");
        RuleFor(x => x.Version).Must(version => version > 0).WithMessage("Version must be the current positive concurrency version.");
    }
}

/// <summary>Updates the non-business probe to manually demonstrate an actionable stale-write conflict.</summary>
public sealed class UpdateFoundationProbeCommandHandler(IFoundationProbeStore store, IUnitOfWork unitOfWork) : IRequestHandler<UpdateFoundationProbeCommand, Result<FoundationProbeResponse>>
{
    /// <inheritdoc />
    public async Task<Result<FoundationProbeResponse>> Handle(UpdateFoundationProbeCommand request, CancellationToken cancellationToken)
    {
        var probe = await store.GetAsync(request.Id, cancellationToken);
        if (probe is null) return Result<FoundationProbeResponse>.Failure(new Error("not_found", "The requested foundation resource was not found.", ErrorType.NotFound));
        probe.Rename(request.Label);
        store.SetExpectedVersion(probe, request.Version);
        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (!saved.IsSuccess) return Result<FoundationProbeResponse>.Failure(saved.Error!);
        return Result<FoundationProbeResponse>.Success(new FoundationProbeResponse(probe.Id, probe.Label, probe.CreatedAt, TimeZoneInfo.ConvertTimeBySystemTimeZoneId(probe.CreatedAt, "Asia/Amman").ToString("yyyy-MM-dd HH:mm zzz"), probe.Version, false));
    }
}

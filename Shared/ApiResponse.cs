namespace Shared;

/// <summary>Standard API envelope for every success and expected failure.</summary>
public sealed record ApiResponse<T>(int Code, string Message, T? Data);

/// <summary>Payload for field-specific validation and conflict details.</summary>
public sealed record ApiErrorData(IReadOnlyDictionary<string, string[]>? Errors = null);

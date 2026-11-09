namespace InquiryService.Api.Contracts;

public sealed record SubmitInquiryRequest(
    string? IdempotencyKey,
    string Payload,
    bool BypassCache = false);

public sealed record InquiryAttemptDto(
    int Number,
    string ProviderName,
    string Outcome,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    long DurationMs,
    string? ErrorCode,
    string? ErrorMessage,
    string? ResponsePayload);

public sealed record InquiryResponse(
    Guid InquiryId,
    string IdempotencyKey,
    string Status,
    string? SelectedProvider,
    string? ResponsePayload,
    string? ErrorCode,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    string ExecutionSource,
    IReadOnlyList<InquiryAttemptDto> Attempts);

using InquiryService.Domain.Inquiries.Enums;

namespace InquiryService.Domain.Inquiries;

public sealed class InquiryAttempt
{
    private InquiryAttempt() { }

    internal InquiryAttempt(
        Guid inquiryId,
        int number,
        string providerName,
        AttemptOutcome outcome,
        DateTimeOffset startedAt,
        DateTimeOffset completedAt,
        string? responsePayload = null,
        string? errorCode = null,
        string? errorMessage = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);

        if (number < 1)
            throw new ArgumentOutOfRangeException(nameof(number));

        if (completedAt < startedAt)
            throw new ArgumentException("زمان پایان نمی‌تواند قبل از شروع باشد.");

        Id = Guid.NewGuid();
        InquiryId = inquiryId;
        Number = number;
        ProviderName = providerName;
        Outcome = outcome;
        StartedAt = startedAt;
        CompletedAt = completedAt;
        ResponsePayload = responsePayload;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public Guid Id { get; private set; }
    public Guid InquiryId { get; private set; }
    public int Number { get; private set; }
    public string ProviderName { get; private set; } = null!;
    public AttemptOutcome Outcome { get; private set; }
    public string? ResponsePayload { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset CompletedAt { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }

    public long DurationMs => (long)(CompletedAt - StartedAt).TotalMilliseconds;
}

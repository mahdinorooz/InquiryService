using InquiryService.Domain.Inquiries.Enums;

namespace InquiryService.Domain.Inquiries;

public sealed class Inquiry
{
    private readonly List<InquiryAttempt> _attempts = [];

    private Inquiry() { }

    private Inquiry(
        string idempotencyKey,
        string requestFingerprint,
        string requestPayload,
        DateTimeOffset createdAt)
    {
        Id = Guid.NewGuid();
        IdempotencyKey = idempotencyKey;
        RequestFingerprint = requestFingerprint;
        RequestPayload = requestPayload;
        CreatedAt = createdAt;
        Status = InquiryStatus.Pending;
    }

    public Guid Id { get; private set; }
    public string IdempotencyKey { get; private set; } = null!;
    public string RequestFingerprint { get; private set; } = null!;
    public string RequestPayload { get; private set; } = null!;
    public InquiryStatus Status { get; private set; }
    public string? ResponsePayload { get; private set; }
    public string? RespondingProvider { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public IReadOnlyList<InquiryAttempt> Attempts => _attempts.AsReadOnly();

    public static Inquiry Start(
        string idempotencyKey,
        string requestFingerprint,
        string requestPayload,
        DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestFingerprint);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestPayload);

        return new Inquiry(idempotencyKey, requestFingerprint, requestPayload, createdAt);
    }

    public void RecordAttempt(
        string providerName,
        AttemptOutcome outcome,
        DateTimeOffset startedAt,
        DateTimeOffset completedAt,
        string? responsePayload = null,
        string? errorCode = null,
        string? errorMessage = null)
    {
        if (Status != InquiryStatus.Pending)
            throw new InvalidOperationException("برای استعلام نهایی‌شده نمی‌توان تلاش جدید ثبت کرد.");

        var attempt = new InquiryAttempt(Id, _attempts.Count + 1, providerName, outcome, startedAt, completedAt, errorCode, errorMessage);
        _attempts.Add(attempt);

        switch (outcome)
        {
            case AttemptOutcome.Succeeded:
                Status = InquiryStatus.Succeeded;
                ResponsePayload = responsePayload;
                RespondingProvider = providerName;
                CompletedAt = completedAt;
                break;
            case AttemptOutcome.BusinessRejected:
                Status = InquiryStatus.BusinessRejected;
                ResponsePayload = responsePayload;
                RespondingProvider = providerName;
                ErrorCode = errorCode;
                ErrorMessage = errorMessage;
                CompletedAt = completedAt;
                break;
        }
    }

    public void Fail(string errorCode, string errorMessage, DateTimeOffset completedAt)
    {
        if (Status != InquiryStatus.Pending) throw new InvalidOperationException("استعلام قبلاً نهایی شده است.");

        Status = InquiryStatus.Failed;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        CompletedAt = completedAt;
    }

    public void CompleteFromCache(
    string providerName,
    string responsePayload,
    DateTimeOffset completedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(responsePayload);

        if (Status != InquiryStatus.Pending)
            throw new InvalidOperationException("استعلام قبلاً نهایی شده است.");

        if (completedAt < CreatedAt)
            throw new ArgumentException("زمان پایان نمی‌تواند قبل از زمان ایجاد باشد.");

        Status = InquiryStatus.Succeeded;
        RespondingProvider = providerName;
        ResponsePayload = responsePayload;
        CompletedAt = completedAt;
    }

}

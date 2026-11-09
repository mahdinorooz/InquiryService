using InquiryService.Domain.Inquiries;

namespace InquiryService.Application.Abstractions;

public interface IInquiryRepository
{
    Task<Inquiry?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<Inquiry?> GetPendingByFingerprintAsync(
        string requestFingerprint,
        CancellationToken cancellationToken);

    Task<Inquiry?> GetRecentSuccessfulByFingerprintAsync(
        string requestFingerprint,
        DateTimeOffset completedAfter,
        CancellationToken cancellationToken);

    Task AddAsync(
        Inquiry inquiry,
        CancellationToken cancellationToken);

    Task<bool> TryAddAsync(
       Inquiry inquiry,
       CancellationToken cancellationToken);

    Task UpdateAsync(
        Inquiry inquiry,
        CancellationToken cancellationToken);
}

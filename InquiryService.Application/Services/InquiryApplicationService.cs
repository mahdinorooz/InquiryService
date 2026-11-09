using InquiryService.Application.Abstractions;
using InquiryService.Domain.Inquiries;
using InquiryService.Domain.Inquiries.Enums;
using System.Security.Cryptography;
using System.Text;

namespace InquiryService.Application.Services;

public enum InquirySubmissionKind
{
    Created,
    Existing,
    Cached,
    InProgress,
    Conflict
}

public sealed record InquirySubmissionResult(
    InquirySubmissionKind Kind,
    Inquiry? Inquiry);

public sealed class InquiryApplicationService
{
    private readonly IInquiryRepository _repository;
    private readonly InquiryCoordinator _coordinator;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _cacheTtl;

    public InquiryApplicationService(
        IInquiryRepository repository,
        InquiryCoordinator coordinator,
        TimeProvider timeProvider,
        TimeSpan cacheTtl)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

        if (cacheTtl < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(cacheTtl));

        _cacheTtl = cacheTtl;
    }

    public async Task<InquirySubmissionResult> SubmitAsync(
        string idempotencyKey,
        string requestPayload,
        bool bypassCache,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestPayload);

        if (idempotencyKey.Length > 128)
            throw new ArgumentException(
                "IdempotencyKey must not exceed 128 characters.",
                nameof(idempotencyKey));

        var fingerprint = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(requestPayload)));

        for (var retry = 0; retry < 5; retry++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var existing = await _repository.GetByIdempotencyKeyAsync(
                idempotencyKey, cancellationToken);

            if (existing is not null)
            {
                return existing.RequestFingerprint == fingerprint
                    ? new InquirySubmissionResult(
                        InquirySubmissionKind.Existing, existing)
                    : new InquirySubmissionResult(
                        InquirySubmissionKind.Conflict, existing);
            }

            var inquiry = Inquiry.Start(
                idempotencyKey,
                fingerprint,
                requestPayload,
                _timeProvider.GetUtcNow());

            if (await _repository.TryAddAsync(inquiry, cancellationToken))
            {
                try
                {
                    if (!bypassCache && _cacheTtl > TimeSpan.Zero)
                    {
                        var completedAfter = _timeProvider.GetUtcNow() - _cacheTtl;

                        var cached = await _repository
                            .GetRecentSuccessfulByFingerprintAsync(
                                fingerprint,
                                completedAfter,
                                cancellationToken);

                        if (cached is not null)
                        {
                            inquiry.CompleteFromCache(
                                cached.RespondingProvider!,
                                cached.ResponsePayload!,
                                _timeProvider.GetUtcNow());

                            await _repository.UpdateAsync(
                                inquiry, cancellationToken);

                            return new InquirySubmissionResult(
                                InquirySubmissionKind.Cached, inquiry);
                        }
                    }

                    await _coordinator.ExecuteAsync(inquiry, cancellationToken);
                    await _repository.UpdateAsync(inquiry, cancellationToken);

                    return new InquirySubmissionResult(
                        InquirySubmissionKind.Created, inquiry);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    if (inquiry.Status == InquiryStatus.Pending)
                    {
                        inquiry.Fail(
                            "REQUEST_CANCELED",
                            "Request execution was canceled.",
                            _timeProvider.GetUtcNow());

                        await _repository.UpdateAsync(
                            inquiry, CancellationToken.None);
                    }

                    throw;
                }
            }

            existing = await _repository.GetByIdempotencyKeyAsync(
                idempotencyKey, cancellationToken);

            if (existing is not null)
            {
                return existing.RequestFingerprint == fingerprint
                    ? new InquirySubmissionResult(
                        InquirySubmissionKind.Existing, existing)
                    : new InquirySubmissionResult(
                        InquirySubmissionKind.Conflict, existing);
            }

            var pending = await _repository.GetPendingByFingerprintAsync(
                fingerprint, cancellationToken);

            if (pending is not null)
            {
                return new InquirySubmissionResult(
                    InquirySubmissionKind.InProgress, pending);
            }

            await Task.Delay(TimeSpan.FromMilliseconds(25), cancellationToken);
        }

        throw new InvalidOperationException(
            "Could not resolve the concurrent inquiry. Please retry.");
    }
}

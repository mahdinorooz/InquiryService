using InquiryService.Application.Abstractions;
using InquiryService.Application.Configuration;
using InquiryService.Domain.Inquiries;
using InquiryService.Domain.Inquiries.Enums;

namespace InquiryService.Application.Services;

public sealed class InquiryCoordinator
{
    private readonly IReadOnlyList<IInquiryProvider> _providers;
    private readonly InquiryExecutionOptions _options;
    private readonly TimeProvider _timeProvider;

    public InquiryCoordinator(
        IEnumerable<IInquiryProvider> providers,
        InquiryExecutionOptions options,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        options.Validate();

        _providers = providers
            .OrderBy(provider => provider.Priority)
            .ThenBy(provider => provider.Name, StringComparer.Ordinal)
            .ToArray();

        if (_providers.Count == 0)
        {
            throw new ArgumentException(
                "At least one inquiry provider must be registered.",
                nameof(providers));
        }

        if (_providers.Any(provider =>
                string.IsNullOrWhiteSpace(provider.Name)))
        {
            throw new ArgumentException(
                "Every provider must have a non-empty name.",
                nameof(providers));
        }

        if (_providers
            .GroupBy(provider => provider.Name, StringComparer.Ordinal)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "Provider names must be unique.",
                nameof(providers));
        }

        _options = options;
        _timeProvider = timeProvider;
    }

    public async Task ExecuteAsync(
        Inquiry inquiry,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(inquiry);

        foreach (var provider in _providers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var startedAt = _timeProvider.GetUtcNow();
            ProviderResult result;

            using var timeoutSource =
                new CancellationTokenSource(_options.ProviderTimeout);

            using var linkedSource =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    timeoutSource.Token);

            try
            {
                result = await provider.InquireAsync(
                    inquiry.RequestPayload,
                    linkedSource.Token);

                if (result is null)
                {
                    result = ProviderResult.TechnicalFailure(
                        "EMPTY_PROVIDER_RESPONSE",
                        "Provider returned no result.");
                }
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
                when (timeoutSource.IsCancellationRequested)
            {
                result = ProviderResult.TimedOut();
            }
            catch (TimeoutException)
            {
                result = ProviderResult.TimedOut();
            }
            catch (OperationCanceledException)
            {
                result = ProviderResult.TechnicalFailure(
                    "PROVIDER_CANCELED",
                    "Provider canceled its operation unexpectedly.");
            }
            catch (Exception)
            {
                result = ProviderResult.TechnicalFailure(
                    "PROVIDER_ERROR",
                    "Provider call failed.");
            }

            var completedAt = _timeProvider.GetUtcNow();
            var outcome = MapOutcome(result.Kind);

            inquiry.RecordAttempt(
                providerName: provider.Name,
                outcome: outcome,
                startedAt: startedAt,
                completedAt: completedAt,
                responsePayload: result.ResponsePayload,
                errorCode: result.ErrorCode,
                errorMessage: result.ErrorMessage);

            if (inquiry.Status != InquiryStatus.Pending)
            {
                return;
            }
        }

        inquiry.Fail(
            errorCode: "ALL_PROVIDERS_FAILED",
            errorMessage: "All configured providers failed.",
            completedAt: _timeProvider.GetUtcNow());
    }

    private static AttemptOutcome MapOutcome(ProviderResultKind kind)
    {
        return kind switch
        {
            ProviderResultKind.Success =>
                AttemptOutcome.Succeeded,

            ProviderResultKind.BusinessError =>
                AttemptOutcome.BusinessRejected,

            ProviderResultKind.TechnicalFailure =>
                AttemptOutcome.TechnicalFailure,

            ProviderResultKind.TimedOut =>
                AttemptOutcome.TimedOut,

            _ => throw new ArgumentOutOfRangeException(
                nameof(kind),
                kind,
                "Unknown provider result kind.")
        };
    }
}

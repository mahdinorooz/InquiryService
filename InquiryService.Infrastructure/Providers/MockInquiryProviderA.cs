using InquiryService.Application.Abstractions;

namespace InquiryService.Infrastructure.Providers;

public sealed class MockInquiryProviderA : IInquiryProvider
{
    public string Name => "ProviderA";
    public int Priority => 1;

    public async Task<ProviderResult> InquireAsync(
        string requestPayload,
        CancellationToken cancellationToken)
    {
        await Task.Delay(100, cancellationToken);

        if (requestPayload.Contains("timeout-a", StringComparison.OrdinalIgnoreCase))
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        }

        if (requestPayload.Contains("tech-error-a", StringComparison.OrdinalIgnoreCase))
        {
            return ProviderResult.TechnicalFailure("PROVIDER_DOWN", "Provider A is temporarily unreachable.");
        }

        if (requestPayload.Contains("business-error-a", StringComparison.OrdinalIgnoreCase))
        {
            return ProviderResult.BusinessError("INVALID_NATIONAL_CODE", "Business rule violation in Provider A.");
        }

        return ProviderResult.Success($"{{\"provider\":\"ProviderA\",\"status\":\"OK\",\"data\":\"{requestPayload}\"}}");
    }
}

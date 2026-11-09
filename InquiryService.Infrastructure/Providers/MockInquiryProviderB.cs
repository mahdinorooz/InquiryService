using InquiryService.Application.Abstractions;

namespace InquiryService.Infrastructure.Providers;

public sealed class MockInquiryProviderB : IInquiryProvider
{
    public string Name => "ProviderB";
    public int Priority => 2;

    public async Task<ProviderResult> InquireAsync(
        string requestPayload,
        CancellationToken cancellationToken)
    {
        await Task.Delay(150, cancellationToken);

        if (requestPayload.Contains("tech-error-b", StringComparison.OrdinalIgnoreCase))
        {
            return ProviderResult.TechnicalFailure("PROVIDER_B_DOWN", "Provider B is temporarily unreachable.");
        }

        if (requestPayload.Contains("business-error-b", StringComparison.OrdinalIgnoreCase))
        {
            return ProviderResult.BusinessError("INVALID_ACCOUNT", "Business rule violation in Provider B.");
        }

        return ProviderResult.Success($"{{\"provider\":\"ProviderB\",\"status\":\"OK\",\"data\":\"{requestPayload}\"}}");
    }
}

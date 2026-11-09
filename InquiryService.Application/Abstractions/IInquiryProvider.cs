namespace InquiryService.Application.Abstractions;

public interface IInquiryProvider
{
    string Name { get; }
    int Priority { get; }

    Task<ProviderResult> InquireAsync(
        string requestPayload,
        CancellationToken cancellationToken);
}

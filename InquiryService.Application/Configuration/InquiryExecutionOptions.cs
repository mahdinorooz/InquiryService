namespace InquiryService.Application.Configuration;

public sealed record InquiryExecutionOptions
{
    public TimeSpan ProviderTimeout { get; init; } = TimeSpan.FromSeconds(2);

    public void Validate()
    {
        if (ProviderTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ProviderTimeout),
                "Provider timeout must be greater than zero.");
        }
    }
}

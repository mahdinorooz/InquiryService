namespace InquiryService.Application.Abstractions;

public enum ProviderResultKind
{
    Success = 1,
    BusinessError = 2,
    TechnicalFailure = 3,
    TimedOut = 4
}

public sealed record ProviderResult
{
    private ProviderResult(
        ProviderResultKind kind,
        string? responsePayload,
        string? errorCode,
        string? errorMessage)
    {
        Kind = kind;
        ResponsePayload = responsePayload;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public ProviderResultKind Kind { get; }
    public string? ResponsePayload { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    public static ProviderResult Success(string responsePayload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(responsePayload);

        return new ProviderResult(
            ProviderResultKind.Success,
            responsePayload,
            errorCode: null,
            errorMessage: null);
    }

    public static ProviderResult BusinessError(
        string errorCode,
        string errorMessage,
        string? responsePayload = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        return new ProviderResult(
            ProviderResultKind.BusinessError,
            responsePayload,
            errorCode,
            errorMessage);
    }

    public static ProviderResult TechnicalFailure(
        string errorCode,
        string errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        return new ProviderResult(
            ProviderResultKind.TechnicalFailure,
            responsePayload: null,
            errorCode,
            errorMessage);
    }

    public static ProviderResult TimedOut()
    {
        return new ProviderResult(
            ProviderResultKind.TimedOut,
            responsePayload: null,
            errorCode: "PROVIDER_TIMEOUT",
            errorMessage: "Provider did not respond before the timeout.");
    }
}

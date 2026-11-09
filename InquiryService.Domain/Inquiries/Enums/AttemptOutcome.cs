namespace InquiryService.Domain.Inquiries.Enums
{
    public enum AttemptOutcome
    {
        Succeeded = 1,
        BusinessRejected = 2,
        TechnicalFailure = 3,
        TimedOut = 4
    }
}

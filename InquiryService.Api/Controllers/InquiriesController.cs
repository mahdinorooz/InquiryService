using InquiryService.Api.Contracts;
using InquiryService.Application.Services;
using InquiryService.Domain.Inquiries;
using InquiryService.Domain.Inquiries.Enums;
using Microsoft.AspNetCore.Mvc;

namespace InquiryService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class InquiriesController : ControllerBase
{
    private readonly InquiryApplicationService _inquiryService;

    public InquiriesController(InquiryApplicationService inquiryService)
    {
        _inquiryService = inquiryService ?? throw new ArgumentNullException(nameof(inquiryService));
    }

    [HttpPost]
    [ProducesResponseType(typeof(InquiryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(InquiryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(InquiryResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(InquiryResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(InquiryResponse), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> SubmitInquiry(
        [FromBody] SubmitInquiryRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? headerIdempotencyKey,
        [FromQuery] bool? bypassCache,
        CancellationToken cancellationToken)
    {
        var effectiveKey = !string.IsNullOrWhiteSpace(headerIdempotencyKey)
            ? headerIdempotencyKey
            : request.IdempotencyKey;

        if (string.IsNullOrWhiteSpace(effectiveKey))
        {
            return BadRequest(new { Error = "Idempotency-Key must be provided in header or request body." });
        }

        if (string.IsNullOrWhiteSpace(request.Payload))
        {
            return BadRequest(new { Error = "Payload cannot be empty." });
        }

        var shouldBypassCache = bypassCache ?? request.BypassCache;

        var result = await _inquiryService.SubmitAsync(
            effectiveKey,
            request.Payload,
            shouldBypassCache,
            cancellationToken);

        if (result.Kind == InquirySubmissionKind.Conflict)
        {
            return Conflict(new
            {
                Error = "The provided Idempotency-Key has already been used with a different request payload."
            });
        }

        if (result.Kind == InquirySubmissionKind.InProgress)
        {
            return Accepted(new
            {
                Status = "InProgress",
                Message = "An identical inquiry is currently being processed.",
                ActiveInquiryId = result.Inquiry?.Id
            });
        }

        var inquiry = result.Inquiry!;
        var responseDto = MapToResponse(inquiry, result.Kind);

        return inquiry.Status switch
        {
            InquiryStatus.Succeeded => result.Kind == InquirySubmissionKind.Created
                ? StatusCode(StatusCodes.Status201Created, responseDto)
                : Ok(responseDto),

            InquiryStatus.BusinessRejected => UnprocessableEntity(responseDto),

            InquiryStatus.Failed => StatusCode(StatusCodes.Status502BadGateway, responseDto),

            _ => Ok(responseDto)
        };
    }

    private static InquiryResponse MapToResponse(Inquiry inquiry, InquirySubmissionKind kind)
    {
        var attempts = inquiry.Attempts.Select(a => new InquiryAttemptDto(
            a.Number,
            a.ProviderName,
            a.Outcome.ToString(),
            a.StartedAt,
            a.CompletedAt,
            a.DurationMs,
            a.ErrorCode,
            a.ErrorMessage,
            a.ResponsePayload
        )).ToList();

        var source = kind switch
        {
            InquirySubmissionKind.Cached => "Cache",
            InquirySubmissionKind.Existing => "ExistingRecord",
            _ => "ProviderExecution"
        };

        return new InquiryResponse(
            inquiry.Id,
            inquiry.IdempotencyKey,
            inquiry.Status.ToString(),
            inquiry.RespondingProvider,
            inquiry.ResponsePayload,
            inquiry.ErrorCode,
            inquiry.ErrorMessage,
            inquiry.CreatedAt,
            inquiry.CompletedAt,
            source,
            attempts
        );
    }
}

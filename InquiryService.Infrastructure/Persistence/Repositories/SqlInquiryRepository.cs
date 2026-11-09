using InquiryService.Application.Abstractions;
using InquiryService.Domain.Inquiries;
using InquiryService.Domain.Inquiries.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace InquiryService.Infrastructure.Persistence.Repositories;

public sealed class SqlInquiryRepository : IInquiryRepository
{
    private readonly InquiryDbContext _context;

    public SqlInquiryRepository(InquiryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Inquiry?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        return await _context.Inquiries
            .Include(x => x.Attempts)
            .FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public Task<Inquiry?> GetPendingByFingerprintAsync(
    string requestFingerprint,
    CancellationToken cancellationToken)
    {
        return _context.Inquiries
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.RequestFingerprint == requestFingerprint
                     && x.Status == InquiryStatus.Pending,
                cancellationToken);
    }

    public Task<Inquiry?> GetRecentSuccessfulByFingerprintAsync(
        string requestFingerprint,
        DateTimeOffset completedAfter,
        CancellationToken cancellationToken)
    {
        return _context.Inquiries
            .AsNoTracking()
            .Where(x => x.RequestFingerprint == requestFingerprint
                        && x.Status == InquiryStatus.Succeeded
                        && x.CompletedAt >= completedAfter
                        && x.RespondingProvider != null
                        && x.ResponsePayload != null
                        && x.Attempts.Any(a =>
                            a.Outcome == AttemptOutcome.Succeeded))
            .OrderByDescending(x => x.CompletedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> TryAddAsync(
        Inquiry inquiry,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(inquiry);

        await _context.Inquiries.AddAsync(inquiry, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException sql
                  && sql.Number is 2601 or 2627
                  && (sql.Message.Contains(
                          "UX_Inquiries_IdempotencyKey",
                          StringComparison.Ordinal)
                      || sql.Message.Contains(
                          "UX_Inquiries_ActiveFingerprint",
                          StringComparison.Ordinal)))
        {
            _context.Entry(inquiry).State = EntityState.Detached;
            return false;
        }
    }

    public async Task AddAsync(
        Inquiry inquiry,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(inquiry);

        await _context.Inquiries.AddAsync(inquiry, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
    Inquiry inquiry,
    CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(inquiry);
        await _context.SaveChangesAsync(cancellationToken);
    }

}

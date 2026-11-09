using InquiryService.Domain.Inquiries;
using Microsoft.EntityFrameworkCore;

namespace InquiryService.Infrastructure.Persistence;

public sealed class InquiryDbContext : DbContext
{
    public InquiryDbContext(DbContextOptions<InquiryDbContext> options)
        : base(options)
    {
    }

    public DbSet<Inquiry> Inquiries => Set<Inquiry>();
    public DbSet<InquiryAttempt> InquiryAttempts => Set<InquiryAttempt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InquiryDbContext).Assembly);
    }
}

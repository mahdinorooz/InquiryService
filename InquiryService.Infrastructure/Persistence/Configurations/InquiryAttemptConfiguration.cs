using InquiryService.Domain.Inquiries;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InquiryService.Infrastructure.Persistence.Configurations;

internal sealed class InquiryAttemptConfiguration : IEntityTypeConfiguration<InquiryAttempt>
{
    public void Configure(EntityTypeBuilder<InquiryAttempt> builder)
    {
        builder.ToTable("InquiryAttempts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.InquiryId)
            .IsRequired();

        builder.Property(x => x.ProviderName)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.Outcome)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(x => x.StartedAt)
            .IsRequired();

        builder.Property(x => x.CompletedAt)
            .IsRequired();

        builder.Property(x => x.ResponsePayload);

        builder.Property(x => x.ErrorCode)
            .HasMaxLength(64);

        builder.Property(x => x.ErrorMessage);

        builder.HasIndex(x => x.InquiryId);
    }
}

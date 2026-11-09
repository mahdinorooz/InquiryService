using InquiryService.Domain.Inquiries;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InquiryService.Infrastructure.Persistence.Configurations;

internal sealed class InquiryConfiguration : IEntityTypeConfiguration<Inquiry>
{
    public void Configure(EntityTypeBuilder<Inquiry> builder)
    {
        builder.ToTable("Inquiries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasIndex(x => x.IdempotencyKey).IsUnique().HasDatabaseName("UX_Inquiries_IdempotencyKey");

        builder.HasIndex(x => x.RequestFingerprint)
            .IsUnique()
            .HasFilter("[Status] = 'Pending'").HasDatabaseName("UX_Inquiries_ActiveFingerprint");


        builder.Property(x => x.RequestPayload)
            .IsRequired();

        builder.Property(x => x.ResponsePayload);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(x => x.RespondingProvider)
            .HasMaxLength(100);


        builder.Property(x => x.ErrorCode)
            .HasMaxLength(64);

        builder.Property(x => x.ErrorMessage);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.CompletedAt);
        builder.HasMany(x => x.Attempts)
            .WithOne()
            .HasForeignKey(x => x.InquiryId)
            .OnDelete(DeleteBehavior.Cascade);

        var navigation = builder.Metadata.FindNavigation(nameof(Inquiry.Attempts));
        navigation?.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

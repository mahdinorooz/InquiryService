using InquiryService.Application.Abstractions;
using InquiryService.Infrastructure.Persistence;
using InquiryService.Infrastructure.Persistence.Repositories;
using InquiryService.Infrastructure.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InquiryService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // کانفیگ دیتابیس SQL Server
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<InquiryDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        // ثبت Repository
        services.AddScoped<IInquiryRepository, SqlInquiryRepository>();

        // ثبت پرووایدرهای استعلام
        services.AddScoped<IInquiryProvider, MockInquiryProviderA>();
        services.AddScoped<IInquiryProvider, MockInquiryProviderB>();

        return services;
    }
}

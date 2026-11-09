using InquiryService.Application.Abstractions;
using InquiryService.Application.Configuration;
using InquiryService.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace InquiryService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var executionOptions = new InquiryExecutionOptions
        {
            ProviderTimeout = TimeSpan.FromSeconds(2)
        };

        services.AddSingleton(executionOptions);
        services.AddScoped<InquiryCoordinator>();

        services.AddScoped<InquiryApplicationService>(sp =>
        {
            var repository = sp.GetRequiredService<IInquiryRepository>();
            var coordinator = sp.GetRequiredService<InquiryCoordinator>();
            var timeProvider = sp.GetRequiredService<TimeProvider>();
            var cacheTtl = TimeSpan.FromMinutes(5);

            return new InquiryApplicationService(repository, coordinator, timeProvider, cacheTtl);
        });

        return services;
    }
}

using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductService.Application.Common.Interfaces;
using ProductService.Domain.Repositories;
using ProductService.Infrastructure.Messaging;
using ProductService.Infrastructure.Persistence;
using ProductService.Infrastructure.Persistence.Repositories;

namespace ProductService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Database ──────────────────────────────────────────────────────────
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            }));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IProductRepository, ProductRepository>();
        // services.AddScoped<IEventPublisher, NoOpEventPublisher>();
        // ── Messaging ─────────────────────────────────────────────────────────
        var serviceBusSettings = configuration
            .GetSection(ServiceBusSettings.SectionName)
            .Get<ServiceBusSettings>();

        var hasServiceBus = !string.IsNullOrWhiteSpace(serviceBusSettings?.ConnectionString);

        if (hasServiceBus)
        {
            services.AddMassTransit(x =>
            {
                x.UsingAzureServiceBus((ctx, cfg) =>
                {
                    cfg.Host(serviceBusSettings!.ConnectionString);
                    cfg.ConfigureEndpoints(ctx);
                });
            });

            services.AddScoped<IEventPublisher, AzureServiceBusEventPublisher>();
        }
        else
        {
            // Fallback for local development without a Service Bus connection
            services.AddScoped<IEventPublisher, NoOpEventPublisher>();
        }

        return services;
    }
}

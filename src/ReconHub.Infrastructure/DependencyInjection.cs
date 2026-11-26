using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ReconHub.Application.Abstractions;
using ReconHub.Application.Abstractions.Persistence;
using ReconHub.Application.Abstractions.Security;
using ReconHub.Application.Detection;
using ReconHub.Infrastructure.Detection;
using ReconHub.Infrastructure.Persistence;
using ReconHub.Infrastructure.Persistence.Repositories;
using ReconHub.Infrastructure.Persistence.Seeding;
using ReconHub.Infrastructure.Security;
using ReconHub.Infrastructure.Tenancy;

namespace ReconHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.TryAddSingleton(TimeProvider.System);

        // Read lazily so tooling (OpenAPI generation, EF tools) and test overrides work without a database.
        services.AddDbContext<ReconHubDbContext>((serviceProvider, options) =>
        {
            var connectionString = serviceProvider.GetRequiredService<IConfiguration>()
                .GetConnectionString(SqlServerConfiguration.ConnectionStringName);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"Connection string '{SqlServerConfiguration.ConnectionStringName}' is not configured.");
            }

            options.UseReconHubSqlServer(connectionString);
        });

        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IIntegrationEventRepository, IntegrationEventRepository>();
        services.AddScoped<IFlowDefinitionRepository, FlowDefinitionRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IAuditLog, AuditLog>();
        services.AddScoped<IExceptionQueries, ExceptionQueries>();
        services.AddScoped<IApiKeyAuthenticator, ApiKeyAuthenticator>();

        services.AddOptions<DetectionOptions>()
            .Bind(configuration.GetSection(DetectionOptions.SectionName))
            .Validate(o => o.Interval > TimeSpan.Zero, "Detection:Interval must be positive.")
            .Validate(o => o.BatchSize > 0, "Detection:BatchSize must be positive.")
            .Validate(o => o.MaxBatchesPerTenant > 0, "Detection:MaxBatchesPerTenant must be positive.")
            .ValidateOnStart();
        services.AddSingleton<IDetectionCycle, DetectionCycle>();
        services.AddHostedService<DetectionWorker>();

        services.AddOptions<SeedOptions>().Bind(configuration.GetSection(SeedOptions.SectionName));
        services.AddScoped<DevelopmentDataSeeder>();

        services.AddHealthChecks().AddDbContextCheck<ReconHubDbContext>("database", tags: ["ready"]);

        return services;
    }
}

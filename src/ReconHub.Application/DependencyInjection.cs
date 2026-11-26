using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ReconHub.Application.Detection;
using ReconHub.Application.Events;
using ReconHub.Application.Exceptions;
using ReconHub.Domain.Detection;

namespace ReconHub.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<IngestEventRequestValidator>(ServiceLifetime.Scoped);
        services.AddSingleton(TransactionEvaluator.Default);

        services.AddScoped<EventIngestionHandler>();
        services.AddScoped<ListExceptionsHandler>();
        services.AddScoped<ResolveExceptionHandler>();
        services.AddScoped<DetectExceptionsHandler>();

        return services;
    }
}

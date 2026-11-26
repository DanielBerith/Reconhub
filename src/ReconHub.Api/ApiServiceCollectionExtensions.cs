using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using ReconHub.Api.Auth;
using ReconHub.Application.Abstractions;

namespace ReconHub.Api;

internal static class ApiServiceCollectionExtensions
{
    public const string FrontendCorsPolicy = "Frontend";

    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddProblemDetails();
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            // Web defaults accept numbers as strings, which also makes OpenAPI type every number as "number | string".
            options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
        });

        services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info.Title = "ReconHub API";
            document.Info.Description = "Integration monitoring and reconciliation for WMS, ERP and carrier systems.";
            return Task.CompletedTask;
        }));

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, ClaimsCurrentUser>();

        // Two caller types: source systems (API key) and support-team operators (JWT bearer).
        // JWT settings bind from Authentication:Schemes:Bearer; `dotnet user-jwts` writes them for local development.
        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.SchemeName, configureOptions: null)
            .AddJwtBearer();
        services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options => options.MapInboundClaims = false);

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicies.SourceSystem, policy => policy
                .AddAuthenticationSchemes(ApiKeyAuthenticationHandler.SchemeName)
                .RequireAuthenticatedUser()
                .RequireClaim(ReconHubClaims.TenantId)
                .RequireClaim(ReconHubClaims.SourceSystemId))
            .AddPolicy(AuthorizationPolicies.Operator, policy => policy
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .RequireClaim(ReconHubClaims.TenantId))
            // Secure by default: an endpoint without explicit auth metadata requires an authenticated operator.
            .SetFallbackPolicy(new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .Build());

        // Only needed when the web app calls the API directly; the Vite dev proxy avoids CORS altogether.
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        services.AddCors(options => options.AddPolicy(FrontendCorsPolicy, policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()));

        return services;
    }
}

internal static class BuildTimeTooling
{
    /// <summary>
    /// True while the build generates the OpenAPI document. The tool runs the entry point with a no-op server,
    /// so side effects such as seeding or the detection worker must be skipped.
    /// </summary>
    public static bool IsGeneratingOpenApiDocument =>
        string.Equals(Assembly.GetEntryAssembly()?.GetName().Name, "GetDocument.Insider", StringComparison.Ordinal);
}

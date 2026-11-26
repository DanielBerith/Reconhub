using System.Globalization;
using ReconHub.Api;
using ReconHub.Api.Auth;
using ReconHub.Api.Endpoints;
using ReconHub.Application;
using ReconHub.Infrastructure;
using ReconHub.Infrastructure.Detection;
using ReconHub.Infrastructure.Persistence.Seeding;
using Serilog;
using Serilog.Formatting.Compact;

// Bootstrap logger for failures before the host is built; replaced by the configured logger below.
Log.Logger = new LoggerConfiguration().WriteTo.Console(formatProvider: CultureInfo.InvariantCulture).CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSerilog((services, logger) =>
    {
        logger.ReadFrom.Configuration(builder.Configuration).ReadFrom.Services(services);
        if (builder.Environment.IsDevelopment())
        {
            logger.WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}",
                formatProvider: CultureInfo.InvariantCulture);
        }
        else
        {
            logger.WriteTo.Console(new RenderedCompactJsonFormatter());
        }
    });

    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration)
        .AddApi(builder.Configuration);

    if (BuildTimeTooling.IsGeneratingOpenApiDocument)
    {
        builder.Services.Configure<DetectionOptions>(options => options.Enabled = false);
    }

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();
    app.UseStatusCodePages();
    app.UseCors(ApiServiceCollectionExtensions.FrontendCorsPolicy);
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseMiddleware<TenantResolutionMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi().AllowAnonymous();
    }

    app.MapHealthEndpoints();
    app.MapEventEndpoints();
    app.MapExceptionEndpoints();

    if (app.Configuration.GetValue<bool>("Seed:Enabled") && !BuildTimeTooling.IsGeneratingOpenApiDocument)
    {
        await app.Services.SeedDevelopmentDataAsync();
    }

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    // HostAbortedException is how EF Core tooling stops the app after building it; it is not a failure.
    Log.Fatal(ex, "ReconHub API terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

/// <summary>Entry point; public so integration tests can use WebApplicationFactory&lt;Program&gt;.</summary>
public partial class Program;

using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ReconHub.Api.Auth;
using ReconHub.Application.Detection;
using ReconHub.Infrastructure.Persistence;
using ReconHub.Infrastructure.Tenancy;
using Testcontainers.MsSql;

namespace ReconHub.IntegrationTests;

/// <summary>
/// Hosts the real API against a throwaway SQL Server container with the real migrations and seed data.
/// The clock is fake so deadlines can be crossed instantly, and the background worker is off so tests
/// run detection cycles explicitly.
/// </summary>
public sealed class ReconHubApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string WmsApiKey = "test-wms-api-key";
    public const string ErpApiKey = "test-erp-api-key";
    public const string OperatorName = "test.operator";

    private static readonly SymmetricSecurityKey JwtSigningKey = new(RandomNumberGenerator.GetBytes(32));

    private readonly MsSqlContainer _database = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 11, 26, 9, 0, 0, TimeSpan.Zero));

    private string ConnectionString =>
        new SqlConnectionStringBuilder(_database.GetConnectionString()) { InitialCatalog = "ReconHubTests" }.ConnectionString;

    public async ValueTask InitializeAsync()
    {
        await _database.StartAsync();

        var options = new DbContextOptionsBuilder<ReconHubDbContext>().UseReconHubSqlServer(ConnectionString).Options;
        await using var db = new ReconHubDbContext(options, new TenantContext());
        await db.Database.MigrateAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }

    public HttpClient CreateSourceSystemClient(string apiKey)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthenticationHandler.HeaderName, apiKey);
        return client;
    }

    public HttpClient CreateOperatorClient(Guid tenantId)
    {
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Claims = new Dictionary<string, object>
            {
                ["sub"] = OperatorName,
                ["name"] = OperatorName,
                [ReconHubClaims.TenantId] = tenantId.ToString("D", CultureInfo.InvariantCulture),
            },
            SigningCredentials = new SigningCredentials(JwtSigningKey, SecurityAlgorithms.HmacSha256),
        });

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public Task<DetectionCycleSummary> RunDetectionAsync(CancellationToken cancellationToken) =>
        Services.GetRequiredService<IDetectionCycle>().RunAsync(cancellationToken);

    public async Task<T> QueryAsync<T>(Guid tenantId, Func<ReconHubDbContext, Task<T>> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        await using var scope = Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<TenantContext>().SetTenant(tenantId);
        return await query(scope.ServiceProvider.GetRequiredService<ReconHubDbContext>());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:ReconHub", ConnectionString);
        builder.UseSetting("Detection:Enabled", "false");
        builder.UseSetting("Seed:Enabled", "true");
        builder.UseSetting("Seed:WmsApiKey", WmsApiKey);
        builder.UseSetting("Seed:ErpApiKey", ErpApiKey);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);

            // Accept tokens signed with the test key, in place of a real identity provider.
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = JwtSigningKey,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false,
                });
        });
    }
}

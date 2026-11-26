using Microsoft.EntityFrameworkCore;

namespace ReconHub.Infrastructure.Persistence;

public static class SqlServerConfiguration
{
    public const string ConnectionStringName = "ReconHub";

    /// <summary>The single place that configures the SQL Server provider, shared by the app, tooling and tests.</summary>
    public static TBuilder UseReconHubSqlServer<TBuilder>(this TBuilder builder, string connectionString)
        where TBuilder : DbContextOptionsBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        builder.UseSqlServer(connectionString, sql =>
        {
            sql.MigrationsHistoryTable("__EFMigrationsHistory", ReconHubDbContext.Schema);
            sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
        });
        return builder;
    }
}

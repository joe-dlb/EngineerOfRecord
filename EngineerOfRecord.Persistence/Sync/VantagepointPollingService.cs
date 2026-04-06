using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EngineerOfRecord.Persistence.Sync;

/// <summary>
/// Background service that periodically polls Vantagepoint's Employee table (dbo.EM)
/// for changes and syncs them into the local EOR database via <see cref="VantagepointSyncHandler"/>.
///
/// Behavior on startup:
/// <list type="bullet">
///   <item>
///     <term>First run (no EOR records synced yet)</term>
///     <description>Full transfer — pulls all active employees from VP.</description>
///   </item>
///   <item>
///     <term>Restart after downtime</term>
///     <description>Catch-up — pulls only employees modified since last sync time.</description>
///   </item>
///   <item>
///     <term>Normal poll tick</term>
///     <description>Same diff query — typically returns 0 rows.</description>
///   </item>
/// </list>
///
/// All three scenarios use the same query. The only variable is the <c>lastSyncTime</c>
/// parameter: <c>DateTime.MinValue</c> for first run, or the max <c>VantagepointLastSynced</c>
/// from the local table for subsequent runs.
///
/// This service reads directly from <c>Vantagepoint_DevOps.dbo.EM</c> using a raw
/// <see cref="SqlConnection"/> — it does NOT go through EF Core for the Vantagepoint side.
/// EFC is only used for writing to the local EOR database (via the sync handler).
/// </summary>
public class VantagepointPollingService(
    VantagepointSyncHandler syncHandler,
    IConfiguration configuration,
    ILogger<VantagepointPollingService> logger) : BackgroundService
{
    /// <summary>
    /// How often to poll for changes. In production this could be configurable.
    /// Employee data changes infrequently — every 15 minutes is more than sufficient.
    /// </summary>
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run immediately on startup to catch up on any changes during downtime.
        await PollVantagepointAsync(stoppingToken);

        // Then poll on a timer.
        using var timer = new PeriodicTimer(PollInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await PollVantagepointAsync(stoppingToken);
        }
    }

    private async Task PollVantagepointAsync(CancellationToken stoppingToken)
    {
        try
        {
            var lastSync = await syncHandler.GetLastSyncTimeAsync();
            var sinceDate = lastSync ?? new DateTime(1753, 1, 1); // SQL Server datetime minimum

            var label = lastSync.HasValue
                ? $"changes since {sinceDate:yyyy-MM-dd HH:mm:ss}"
                : "full initial load";

            logger.LogInformation("VP Sync: polling for {Label}", label);

            var employees = await QueryVantagepointEmployeesAsync(sinceDate, stoppingToken);

            if (employees.Count == 0)
            {
                logger.LogInformation("VP Sync: no changes found");
                return;
            }

            var synced = await syncHandler.SyncBatchAsync(employees);
            logger.LogInformation("VP Sync: {Found} changed in VP, {Synced} matched local EOR records",
                employees.Count, synced);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Log and continue — a failed poll shouldn't crash the app.
            // Next poll will retry automatically.
            logger.LogError(ex, "VP Sync: polling failed, will retry next cycle");
        }
    }

    /// <summary>
    /// Queries Vantagepoint_DevOps.dbo.EM for active employees modified after the given date.
    /// Uses a raw SqlConnection — this is the ONE place we read from Vantagepoint directly.
    ///
    /// Uses a separate connection string ("Vantagepoint") pointing to the VP server (DLB-SQL04).
    /// In production, this could be replaced with Vantagepoint's REST API (GET /api/employee).
    /// </summary>
    private async Task<List<VantagepointSyncHandler.VantagepointEmployee>> QueryVantagepointEmployeesAsync(
        DateTime sinceDate,
        CancellationToken stoppingToken)
    {
        var connectionString = configuration.GetConnectionString("Vantagepoint")
            ?? throw new InvalidOperationException("Connection string 'Vantagepoint' not found.");

        var employees = new List<VantagepointSyncHandler.VantagepointEmployee>();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(stoppingToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Employee, FirstName, LastName, PreferredName, EMail, Title
            FROM dbo.EM
            WHERE Status = 'A' AND ModDate > @SinceDate
            ORDER BY ModDate DESC
            """;
        command.Parameters.AddWithValue("@SinceDate", sinceDate);

        await using var reader = await command.ExecuteReaderAsync(stoppingToken);
        while (await reader.ReadAsync(stoppingToken))
        {
            employees.Add(new VantagepointSyncHandler.VantagepointEmployee(
                EmployeeId: reader.GetString(0).Trim(),
                FirstName: reader.IsDBNull(1) ? "" : reader.GetString(1).Trim(),
                LastName: reader.IsDBNull(2) ? "" : reader.GetString(2).Trim(),
                PreferredName: reader.IsDBNull(3) ? "" : reader.GetString(3).Trim(),
                Email: reader.IsDBNull(4) ? "" : reader.GetString(4).Trim(),
                Title: reader.IsDBNull(5) ? "" : reader.GetString(5).Trim()));
        }

        return employees;
    }
}

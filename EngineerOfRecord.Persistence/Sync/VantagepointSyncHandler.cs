using EngineerOfRecord.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EngineerOfRecord.Persistence.Sync;

/// <summary>
/// Shared handler for synchronizing employee data from Vantagepoint into the local Employee table.
///
/// Both the polling <see cref="VantagepointPollingService"/> and the webhook endpoint
/// call this same handler. The only difference is what triggers them:
/// - Polling: a timer checks for changes on a schedule
/// - Webhook: Vantagepoint POSTs to our endpoint when data changes
///
/// This handler syncs ALL active employees unconditionally — it doesn't care whether
/// an employee is an EOR or not. The Employee table is a local mirror of Vantagepoint.
/// </summary>
public class VantagepointSyncHandler(
    IDbContextFactory<EorDbContext> dbFactory,
    ILogger<VantagepointSyncHandler> logger)
{
    /// <summary>
    /// Data transfer object representing an employee record from Vantagepoint.
    /// Used by both the polling service (which reads from EM table) and
    /// the webhook endpoint (which receives this as a POST payload).
    /// </summary>
    public record VantagepointEmployee(
        string EmployeeId,
        string FirstName,
        string LastName,
        string PreferredName,
        string Email,
        string Title);

    /// <summary>
    /// Syncs a single employee into the local Employee table.
    /// If the employee exists locally, their fields are updated.
    /// If not, they are inserted. This is an upsert.
    /// </summary>
    public async Task SyncEmployeeAsync(VantagepointEmployee data)
    {
        using var db = await dbFactory.CreateDbContextAsync();

        var existing = await db.Employees.FindAsync(data.EmployeeId);

        if (existing is not null)
        {
            existing.FirstName = data.FirstName;
            existing.LastName = data.LastName;
            existing.PreferredName = data.PreferredName;
            existing.Email = data.Email;
            existing.Title = data.Title;
            existing.LastSynced = DateTime.UtcNow;
        }
        else
        {
            db.Employees.Add(new Employee
            {
                VantagepointEmployeeId = data.EmployeeId,
                FirstName = data.FirstName,
                LastName = data.LastName,
                PreferredName = data.PreferredName,
                Email = data.Email,
                Title = data.Title,
                LastSynced = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();

#pragma warning disable CA1873 // Avoid potentially expensive logging
        logger.LogDebug("Synced VP employee {EmployeeId} ({Name})",
            data.EmployeeId, $"{data.PreferredName} {data.LastName}");
#pragma warning restore CA1873 // Avoid potentially expensive logging
    }

    /// <summary>
    /// Syncs a batch of employees. Returns how many were upserted.
    /// </summary>
    public async Task<int> SyncBatchAsync(IEnumerable<VantagepointEmployee> employees)
    {
        var count = 0;
        foreach (var employee in employees)
        {
            await SyncEmployeeAsync(employee);
            count++;
        }
        return count;
    }

    /// <summary>
    /// Gets the most recent sync timestamp across all Employee records.
    /// Returns null if no records have been synced yet (triggers full initial load).
    /// </summary>
    public async Task<DateTime?> GetLastSyncTimeAsync()
    {
        using var db = await dbFactory.CreateDbContextAsync();

        return await db.Employees
            .Where(e => e.LastSynced != null)
            .MaxAsync(e => (DateTime?)e.LastSynced);
    }
}

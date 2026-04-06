namespace EngineerOfRecord.Domain;

/// <summary>
/// A local copy of an employee record from Deltek Vantagepoint (dbo.EM table).
///
/// This table mirrors all active employees from Vantagepoint. It is maintained by
/// a background sync service that polls VP for changes. The application never writes
/// to Vantagepoint — this is a read-only local cache that keeps the app functional
/// even when Vantagepoint is unavailable.
///
/// Other entities (e.g., <see cref="EngineerOfRecord"/>, future Project entity) reference
/// this table via foreign key rather than duplicating employee data.
/// </summary>
public class Employee
{
    /// <summary>
    /// The employee identifier in Vantagepoint (EM.Employee column).
    /// Zero-padded 6-character string (e.g., "002124").
    /// This is the primary key — one local record per VP employee.
    /// </summary>
    public string VantagepointEmployeeId { get; init; } = string.Empty;

    /// <summary>
    /// Employee's first name (EM.FirstName).
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Employee's last name (EM.LastName).
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Employee's preferred/display name (EM.PreferredName).
    /// For example, "Don" instead of "Donald". Use this for UI display.
    /// </summary>
    public string PreferredName { get; set; } = string.Empty;

    /// <summary>
    /// Employee's email address (EM.EMail).
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Employee's job title (EM.Title).
    /// Examples: "Electrical Engineer III", "CFD Engineer II".
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// When this employee's data was last synchronized from Vantagepoint.
    /// </summary>
    public DateTime? LastSynced { get; set; }

    // ────────────────────────────────────────────────────────────
    //  Computed
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// Convenience property for display. Returns preferred name + last name,
    /// or first name + last name if no preferred name is set.
    /// </summary>
    public string DisplayName =>
        string.IsNullOrWhiteSpace(PreferredName)
            ? $"{FirstName} {LastName}".Trim()
            : $"{PreferredName} {LastName}".Trim();
}

namespace EngineerOfRecord.Domain;

/// <summary>
/// Represents an Engineer of Record (EOR) — a licensed professional engineer authorized
/// to stamp and sign off on engineering deliverables for data center infrastructure projects.
///
/// This entity contains only domain-specific data: discipline, licensing, and active status.
/// Employee information (name, email, title) lives in the <see cref="Employee"/> entity,
/// linked via <see cref="VantagepointEmployeeId"/>.
/// </summary>
public class EngineerOfRecord
{
    // ────────────────────────────────────────────────────────────
    //  Identity
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// Primary key. Generated in code using <see cref="Guid.CreateVersion7()"/> which produces
    /// sequential, timestamp-based GUIDs — globally unique without index fragmentation.
    /// </summary>
    public Guid Id { get; private set; } = Guid.CreateVersion7();

    // ────────────────────────────────────────────────────────────
    //  Employee relationship
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// Foreign key to the local <see cref="Employee"/> table (synced from Vantagepoint).
    /// Links this EOR to their employee record for name, email, and title.
    /// </summary>
    public string VantagepointEmployeeId { get; init; } = string.Empty;

    /// <summary>
    /// Navigation property to the employee's data. Populated by EF Core via
    /// <c>.Include(e => e.Employee)</c>. This is the relationship that replaces
    /// duplicating VP fields directly on this entity.
    /// </summary>
    public Employee Employee { get; init; } = null!;

    // ────────────────────────────────────────────────────────────
    //  Domain fields — owned by this application
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// The engineering discipline this EOR is qualified to stamp.
    /// Determines which project assignment slot (Electrical, Mechanical, or Multi) they can fill.
    /// </summary>
    public Discipline Discipline { get; set; }

    /// <summary>
    /// The date this EOR's professional engineering license expires.
    /// An EOR with an expired license cannot be assigned to new projects
    /// and should be flagged on any active assignments.
    /// </summary>
    public DateTime LicenseExpiration { get; set; }

    /// <summary>
    /// Whether this EOR is currently available for project assignments.
    /// An inactive EOR retains their record and history but won't appear
    /// in assignment dropdowns or workload views.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// The US states in which this EOR holds a valid professional engineering license.
    /// A project in a given state can only be assigned an EOR licensed in that state.
    /// Stored as a JSON array of <see cref="UsState"/> values in the database.
    /// Duplicates are prevented by application logic (UI checkboxes).
    /// </summary>
    public List<UsState> LicensedStates { get; set; } = [];

    // ────────────────────────────────────────────────────────────
    //  Computed
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// Whether this EOR's license has expired as of the current date.
    /// </summary>
    public bool IsLicenseExpired => LicenseExpiration < DateTime.Today;
}

/// <summary>
/// Engineering discipline that an EOR is qualified to stamp.
/// Determines which assignment slot on a project the EOR can fill.
/// </summary>
public enum Discipline
{
    /// <summary>Electrical engineering — power distribution, lighting, fire alarm, etc.</summary>
    Electrical,

    /// <summary>Mechanical engineering — HVAC, plumbing, fire protection, etc.</summary>
    Mechanical,

    /// <summary>Multi-discipline — qualified to stamp across both electrical and mechanical.</summary>
    Multi
}

/// <summary>
/// US state abbreviations. Used for EOR license jurisdiction tracking.
/// An EOR can only be assigned to a project in a state where they hold a valid license.
/// </summary>
public enum UsState
{
    AL, AK, AZ, AR, CA, CO, CT, DE, FL, GA,
    HI, ID, IL, IN, IA, KS, KY, LA, ME, MD,
    MA, MI, MN, MS, MO, MT, NE, NV, NH, NJ,
    NM, NY, NC, ND, OH, OK, OR, PA, RI, SC,
    SD, TN, TX, UT, VT, VA, WA, WV, WI, WY
}

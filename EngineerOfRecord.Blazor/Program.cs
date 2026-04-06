using Microsoft.EntityFrameworkCore;
using EngineerOfRecord.Blazor.Components;
using EngineerOfRecord.Persistence;
using EngineerOfRecord.Persistence.Sync;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register EorDbContext as a factory — the recommended pattern for Blazor Server.
// Blazor circuits are long-lived, so we create short-lived DbContext instances per operation
// instead of injecting a single scoped DbContext that could go stale.
var connectionString = builder.Configuration.GetConnectionString("EorDatabase")
    ?? throw new InvalidOperationException("Connection string 'EorDatabase' not found in configuration.");

builder.Services.AddDbContextFactory<EorDbContext>(options =>
    options.UseSqlServer(connectionString));

// Vantagepoint sync: shared handler + background polling service.
// The handler is also used by the webhook endpoint — one method, two triggers.
builder.Services.AddSingleton<VantagepointSyncHandler>();
builder.Services.AddHostedService<VantagepointPollingService>();

var app = builder.Build();

// Auto-apply migrations on startup (dev/demo only).
// This means anyone who clones the repo just hits F5 — the database
// and tables are created automatically on first run.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IDbContextFactory<EorDbContext>>().CreateDbContext();
    await db.Database.MigrateAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// ── Vantagepoint Webhook Endpoint ────────────────────────────────────────────
// This is how Vantagepoint would push employee changes to us in real-time.
// Configured in Vantagepoint at: Settings > Workflow > Application Workflows
// with a webhook action pointing to this URL.
//
// In production, you'd add authentication (the secret/public key pair that
// Vantagepoint sends in the webhook config). For the demo, this is open.
//
// To test manually:
//   curl -X POST https://localhost:{port}/api/vp-webhook/employee \
//     -H "Content-Type: application/json" \
//     -d '{"Employee":"001885","FirstName":"Devora","LastName":"Abreu","PreferredName":"Devora","EMail":"dabreu@dlbassociates.com","Title":"Electrical Engineer III"}'
//
app.MapPost("/api/vp-webhook/employee", async (
    VantagepointWebhookPayload payload,
    VantagepointSyncHandler syncHandler) =>
{
    // Same handler the polling service uses — one method, two triggers.
    // With the Employee table approach, every employee is upserted unconditionally.
    var employee = new VantagepointSyncHandler.VantagepointEmployee(
        EmployeeId: payload.Employee,
        FirstName: payload.FirstName ?? "",
        LastName: payload.LastName ?? "",
        PreferredName: payload.PreferredName ?? "",
        Email: payload.EMail ?? "",
        Title: payload.Title ?? "");

    await syncHandler.SyncEmployeeAsync(employee);

    return Results.Ok(new { status = "synced", employeeId = payload.Employee });
});

app.Run();

/// <summary>
/// Mirrors the shape of what Vantagepoint sends in its webhook payload.
/// Field names match the EM table columns so Vantagepoint's webhook
/// argument grid can pass them directly without mapping.
/// </summary>
record VantagepointWebhookPayload(
    string Employee,
    string? FirstName,
    string? LastName,
    string? PreferredName,
    string? EMail,
    string? Title);

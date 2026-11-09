using Normora.Api.Middleware;
using Normora.Api.Extensions;
using Normora.Api.Services;
using Normora.Infrastructure.Extensions;
using Normora.Infrastructure.Hubs;
using Normora.Infrastructure.Services;
using Normora.Shared.Interfaces;
using Hangfire;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Duende.Bff;
using Duende.Bff.EntityFramework;
using System.Net;
using Normora.Shared.Constants;

var isMigrationRun = args.Contains("--migrate", StringComparer.OrdinalIgnoreCase);

// Migrations run only in a dedicated one-off deployment process:
// dotnet Normora.Api.dll --migrate
// Run via a lean console host to isolate schema migration from web middleware,
// Kestrel, OpenTelemetry activity listeners, SignalR, and background services
// which can cause SIGSEGV (exit code 139) during container teardown in Linux Docker.
if (isMigrationRun)
{
    var migrationBuilder = Host.CreateApplicationBuilder(args);
    migrationBuilder.Services.AddConfigurationServices(migrationBuilder.Configuration);
    migrationBuilder.Services.AddDatabaseServices(
        migrationBuilder.Configuration,
        migrationBuilder.Environment,
        migrationMode: true);
    migrationBuilder.Services.AddScoped<DatabaseMigrationRunner>();

    // Register SessionDbContext strictly for SessionDatabaseMigrator without Duende background processes
    migrationBuilder.Services.AddDbContext<SessionDbContext>(options =>
    {
        options.UseNpgsql(
            migrationBuilder.Configuration.GetConnectionString("DefaultConnection"),
            sql => sql.MigrationsAssembly("Normora.Api"));
    });
    migrationBuilder.Services.AddScoped<IDatabaseMigrator, SessionDatabaseMigrator>();

    using var migrationHost = migrationBuilder.Build();
    await using (var scope = migrationHost.Services.CreateAsyncScope())
    {
        var runner = scope.ServiceProvider.GetRequiredService<DatabaseMigrationRunner>();
        await runner.MigrateAsync();
    }
    return;
}

var builder = WebApplication.CreateBuilder(args);

// Prevent Kestrel from broadcasting "Server: Kestrel" in HTTP response headers
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
});

// 1. Dependency Injection Configuration
// Separated into extension methods by architectural layer to keep Program.cs clean.
builder.Services.AddConfigurationServices(builder.Configuration); // Options pattern and typed configs
builder.Services.AddDatabaseServices(
    builder.Configuration,
    builder.Environment,
    migrationMode: false); // EF Core
builder.Services.AddExternalProviders(builder.Configuration, builder.Environment); // Storage, Extraction, AI
builder.Services.AddApplicationServices();                   // MediatR, FluentValidation, Scoped Services
builder.Services.AddApiServices(builder.Configuration);      // Controllers, CORS, OpenAPI, Exception Handling
builder.Services.AddIdentityServices(builder.Configuration, builder.Environment); // Keycloak JWT Authentication
builder.Services.AddNormoraTelemetry(builder.Environment);   // OpenTelemetry Tracing & Metrics

var app = builder.Build();

// Add missing HTTP security headers for backend API responses (OWASP ZAP Remediations)
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    await next();
});

// 2. HTTP Request Pipeline Configuration

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // API documentation UI
}

// Global Exception Handler interceptor (returns ProblemDetails JSON instead of crashing)
app.UseExceptionHandler(); 

if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}

// Trust forwarded headers only from explicitly configured reverse proxies.
var trustedProxyAddresses = builder.Configuration
    .GetSection("ReverseProxy:KnownProxies")
    .Get<string[]>() ?? [];
if (!app.Environment.IsDevelopment() && trustedProxyAddresses.Length == 0)
{
    throw new InvalidOperationException(ApiMessages.TrustedProxyRequired);
}

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto,
    ForwardLimit = 1
};
foreach (var trustedProxyAddress in trustedProxyAddresses)
{
    if (!IPAddress.TryParse(trustedProxyAddress, out var address))
    {
        throw new InvalidOperationException(ApiMessages.InvalidTrustedProxy);
    }

    forwardedHeadersOptions.KnownProxies.Add(address);
}

app.UseForwardedHeaders(forwardedHeadersOptions);

app.UseCors("CorsPolicy");

// Security & Tenancy Pipeline (Order is critical for security invariants):
// 1. Authenticate user credentials/session cookies first so ClaimsPrincipal is populated.
app.UseAuthentication();              

// 2. Process BFF session validation and anti-forgery headers.
app.UseBff();

// 3. Rate limiting MUST execute AFTER authentication & BFF session resolution:
//    Partitioners inspect `context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value` ('sub').
//    If executed before authentication, all authenticated requests would lack claims and collapse
//    into the shared anonymous rate-limit bucket, resulting in platform-wide DoS for legitimate users.
app.UseRateLimiter();

// 4. Extract and validate active workspace boundaries (X-Tenant-Id header) for the authenticated principal.
app.UseMiddleware<TenantResolutionMiddleware>();

// 5. Apply endpoint-specific authorization rules (e.g. [Authorize], [RequireTenant]).
app.UseAuthorization();               

app.MapHealthChecks("/healthz").AllowAnonymous();
app.MapControllers().AsBffApiEndpoint(); // Enforces CSRF protection
app.MapBffManagementEndpoints(); // Adds /bff/login, /bff/logout, /bff/user
app.MapHub<DocumentHub>("/hubs/documents").AsBffApiEndpoint().SkipAntiforgery();
app.MapHub<NotificationHub>("/hubs/notifications").AsBffApiEndpoint().SkipAntiforgery();

app.Run();

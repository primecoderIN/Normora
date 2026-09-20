using Normora.Api.Extensions;
using Normora.Api.Middleware;
using Normora.Api.Hubs;
using Hangfire;
using Scalar.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Persistence;
using Normora.Modules.Documents.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Duende.Bff;

var builder = WebApplication.CreateBuilder(args);

// 1. Dependency Injection Configuration
// Separated into extension methods by architectural layer to keep Program.cs clean.
builder.Services.AddConfigurationServices(builder.Configuration); // Options pattern and typed configs
builder.Services.AddDatabaseServices(builder.Configuration); // EF Core, MinIO
builder.Services.AddApplicationServices();                   // MediatR, FluentValidation, Scoped Services
builder.Services.AddApiServices(builder.Configuration);      // Controllers, CORS, OpenAPI, Exception Handling
builder.Services.AddIdentityServices(builder.Configuration, builder.Environment); // Keycloak JWT Authentication
builder.Services.AddNormoraTelemetry(builder.Environment);   // OpenTelemetry Tracing & Metrics

var app = builder.Build();

// 2. HTTP Request Pipeline Configuration

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // API documentation UI
}

// Automatically apply EF Core Migrations on startup
using (var scope = app.Services.CreateScope())
{
    var documentsDbContext = scope.ServiceProvider.GetRequiredService<DocumentsDbContext>();
    documentsDbContext.Database.Migrate();

    var tenantsDbContext = scope.ServiceProvider.GetRequiredService<TenantsDbContext>();
    tenantsDbContext.Database.Migrate();

    var conversationsDbContext = scope.ServiceProvider.GetRequiredService<Normora.Modules.Conversations.Persistence.ConversationsDbContext>();
    conversationsDbContext.Database.Migrate();

    var sessionDbContext = scope.ServiceProvider.GetRequiredService<Duende.Bff.EntityFramework.SessionDbContext>();
    sessionDbContext.Database.Migrate();
}

// Global Exception Handler interceptor (returns ProblemDetails JSON instead of crashing)
app.UseExceptionHandler(); 

if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}

// Trust the X-Forwarded-* headers from nginx so the API knows the public-facing
// host/scheme (localhost:4200) and can build correct OIDC redirect URIs.
// NOTE: KnownNetworks and KnownProxies must be cleared because by default ASP.NET Core
// only trusts loopback proxies. In Docker, nginx runs on an internal network (172.x.x.x),
// which is not loopback and would otherwise be silently ignored.
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

app.UseCors("CorsPolicy");
app.UseRateLimiter();

// Security & Tenancy Pipeline (Order is critical here)
// First, verify the user's session cookie is valid (Authentication).
app.UseAuthentication();              

app.UseBff();

// Second, extract the X-Tenant-Id header and verify the authenticated user has access to that tenant.
app.UseMiddleware<TenantResolutionMiddleware>();

// Finally, apply endpoint-specific authorization rules (e.g. [RequireTenant]).
app.UseAuthorization();               

app.MapControllers().AsBffApiEndpoint(); // Enforces CSRF protection
app.MapBffManagementEndpoints(); // Adds /bff/login, /bff/logout, /bff/user
app.MapHub<DocumentHub>("/hubs/documents").AsBffApiEndpoint();
app.MapHub<NotificationHub>("/hubs/notifications").AsBffApiEndpoint();

app.Run();

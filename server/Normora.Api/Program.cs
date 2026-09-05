using Normora.Api.Extensions;
using Normora.Api.Middleware;
using Normora.Api.Hubs;
using Hangfire;
using Scalar.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Persistence;
using Normora.Modules.Documents.Persistence;

var builder = WebApplication.CreateBuilder(args);

// 1. Dependency Injection Configuration
// Separated into extension methods by architectural layer to keep Program.cs clean.
builder.Services.AddDatabaseServices(builder.Configuration); // EF Core, MinIO
builder.Services.AddApplicationServices();                   // MediatR, FluentValidation, Scoped Services
builder.Services.AddApiServices(builder.Configuration);      // Controllers, CORS, OpenAPI, Exception Handling
builder.Services.AddIdentityServices(builder.Configuration); // Keycloak JWT Authentication

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
}

// Global Exception Handler interceptor (returns ProblemDetails JSON instead of crashing)
app.UseExceptionHandler(); 

if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}

app.UseCors("CorsPolicy");

// Security & Tenancy Pipeline (Order is critical here)
// First, verify the user's JWT token is valid (Authentication).
app.UseAuthentication();              

// Second, extract the X-Tenant-Id header and verify the authenticated user has access to that tenant.
app.UseMiddleware<TenantResolutionMiddleware>();

// Finally, apply endpoint-specific authorization rules (e.g. [RequireTenant]).
app.UseAuthorization();               

app.MapControllers();
app.MapHub<DocumentHub>("/hubs/documents");

app.Run();

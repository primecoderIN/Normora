using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Minio;
using Normora.Infrastructure;
using System.Reflection;
using System.Security.Claims;
using Normora.Api.Infrastructure;

// 1. Create the builder which sets up the configuration and services for the application.
var builder = WebApplication.CreateBuilder(args);

// Register application services via extension method
builder.Services.AddApplicationServices();

// Register Entity Framework Core with PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=localhost;Database=normoradb;Username=postgres;Password=password";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Register MinIO Client
var minioEndpoint = builder.Configuration["Minio:Endpoint"] ?? "localhost:9000";
var minioAccessKey = builder.Configuration["Minio:AccessKey"] ?? "admin";
var minioSecretKey = builder.Configuration["Minio:SecretKey"] ?? "password";

builder.Services.AddMinio(configureClient => configureClient
    .WithEndpoint(minioEndpoint)
    .WithCredentials(minioAccessKey, minioSecretKey)
    .WithSSL(false)
    .Build());

// Register custom services
builder.Services.AddScoped<IDocumentStorageService, MinioDocumentStorageService>();

// 2. Configure Authentication (Who are you?)
// We tell the API to expect a JWT (JSON Web Token) in the Authorization header,
// issued and signed by our Keycloak realm.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // The external URL used to validate the token's 'iss' claim
        var authority = builder.Configuration["Authentication:Authority"] ?? "http://localhost:8080/realms/normora";
        
        // The internal URL used by the API to fetch the public signing keys
        var metadataAddress = builder.Configuration["Authentication:MetadataAddress"] ?? $"{authority}/.well-known/openid-configuration";

        options.Authority = authority;
        options.MetadataAddress = metadataAddress;

        // SECURITY: In production this MUST be true (tokens travel over HTTPS).
        // For local development only, we allow HTTP.
        options.RequireHttpsMetadata = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            // SECURITY FIX: Validate the audience claim.
            // Keycloak adds 'account' to the audience of every token by default.
            // Without this, a token minted for a completely different application
            // would be accepted by this API.
            ValidateAudience = true,
            ValidAudiences = new[] { "account" },

            // SECURITY FIX: Validate the issuer so tokens from a different Keycloak
            // realm or a different server are rejected outright.
            ValidateIssuer = true,
            ValidIssuer = authority,

            // Map 'preferred_username' from Keycloak into User.Identity.Name
            NameClaimType = "preferred_username",
            RoleClaimType = ClaimTypes.Role
        };

        // SECURITY FIX: Keycloak puts roles inside a nested JSON object called 'realm_access'.
        // ASP.NET Core expects roles to be in a flat array of 'role' claims.
        // We intercept the token validation and map them manually.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                if (context.Principal?.Identity is ClaimsIdentity identity)
                {
                    var realmAccessClaim = context.Principal.FindFirst("realm_access")?.Value;
                    if (!string.IsNullOrEmpty(realmAccessClaim))
                    {
                        using var jsonDoc = System.Text.Json.JsonDocument.Parse(realmAccessClaim);
                        if (jsonDoc.RootElement.TryGetProperty("roles", out var rolesElement))
                        {
                            foreach (var role in rolesElement.EnumerateArray())
                            {
                                identity.AddClaim(new Claim(ClaimTypes.Role, role.GetString() ?? string.Empty));
                            }
                        }
                    }
                }
                return Task.CompletedTask;
            }
        };
    });

// 3. Configure Authorization (What are you allowed to do?)
builder.Services.AddAuthorization();

// 4. Configure CORS (Cross-Origin Resource Sharing)
// SECURITY FIX: Using a named allow-list instead of AllowAnyOrigin().
// AllowAnyOrigin() would let any website on the internet make API calls on
// behalf of a logged-in user. We restrict to our Angular app origin only.
builder.Services.AddCors(options =>
{
    options.AddPolicy("NormoraClientOrigins", policy =>
    {
        policy
            // Only our Angular dev server and the containerised client are allowed
            .WithOrigins(
                "http://localhost:4200",   // Angular dev server
                "http://localhost"         // Containerised Nginx client
            )
            // Only the HTTP methods the API actually uses
            .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
            .AllowAnyHeader()
            // Allow the browser to send the Authorization header (for Bearer tokens)
            .AllowCredentials();
    });
});

// 5. Build the application pipeline
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// 6. Apply Middleware — ORDER IS CRITICAL
app.UseExceptionHandler(); // Use the global exception handler
app.UseCors("NormoraClientOrigins"); // Allow the request through CORS first
app.UseAuthentication();              // Identify who the user is
app.UseAuthorization();               // Decide what they can access

app.MapControllers();                 // Map controller endpoints

// 8. Start listening for requests
app.Run();


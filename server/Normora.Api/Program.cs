using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

// 1. Create the builder which sets up the configuration and services for the application.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// 2. Configure Authentication (Who are you?)
// We are telling the ASP.NET Core API to expect a JWT (JSON Web Token) in the Authorization header.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // 'Authority' is the address of the server that issued the token (Keycloak).
        // The API will contact this address to download the public keys needed to verify 
        // that the token hasn't been tampered with.
        options.Authority = "http://localhost:8080/realms/normora";
        
        // In production, this MUST be true to ensure tokens aren't intercepted over plain HTTP.
        // For local development, we set it to false since we aren't using SSL.
        options.RequireHttpsMetadata = false; 
        
        // Configure how strictly we want to validate the token
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // We turn off Audience validation for local dev to keep things simple.
            // In production, we'd verify the token was specifically minted for this API.
            ValidateAudience = false, 
            
            // This maps the 'preferred_username' claim from Keycloak into the .NET User.Identity.Name property
            NameClaimType = "preferred_username"
        };
    });

// 3. Configure Authorization (What are you allowed to do?)
// We add the authorization services to the container.
builder.Services.AddAuthorization();

// 4. Configure CORS (Cross-Origin Resource Sharing)
// Browsers block requests from one domain (e.g., localhost:80) to another (localhost:5000) for security.
// This policy explicitly tells the browser that our Angular app is allowed to talk to this API.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 5. Build the application pipeline
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// 6. Apply Middleware to the pipeline
// The order here is VERY important!
app.UseCors("AllowAll"); // First allow the request through CORS
app.UseAuthentication(); // Then figure out who the user is
app.UseAuthorization();  // Then decide if they have permission to access the endpoint

// 7. Define our API Endpoints
// This is a Minimal API endpoint. The [Authorize] requirement means the request MUST have a valid token.
app.MapGet("/api/me", (System.Security.Claims.ClaimsPrincipal user) =>
{
    // We return a list of all the 'claims' (pieces of information about the user) inside the token
    return user.Claims.Select(c => new { c.Type, c.Value });
})
.RequireAuthorization()
.WithName("GetMe");

// 8. Start listening for requests
app.Run();

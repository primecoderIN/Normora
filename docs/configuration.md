# Configuration Architecture

Normora uses the **Options Pattern** (via `IOptions<T>`) to provide strongly-typed configuration across the API and modular boundaries. This document explains our configuration architecture, reference, environment-specific guidelines, and developer conventions.

## 1. Architecture

We avoid accessing configuration directly through `IConfiguration` or "magic strings". Instead, we bind configuration sections in `appsettings.json` (or environment variables) to strongly-typed C# classes. 

### Why the Options Pattern?
* **Type Safety & IntelliSense:** Developers get autocompletion and compile-time checks instead of string dictionaries.
* **Separation of Concerns:** Services only depend on the specific settings they need (`IOptions<T>`), not the entire application configuration.
* **Validation:** Settings can be validated on application startup.
* **Default Values:** We establish default values directly in the Options classes, removing hardcoded defaults from service logic.

### Organization
* **Shared Options:** Options that span multiple modules or the API (e.g., `AppOptions`, `GeminiOptions`, `KeycloakOptions`) reside in `Normora.Shared/Options`. 
* **Registration:** All shared options are registered in `Normora.Api/Extensions/ConfigurationServiceExtensions.cs`.
* **Constants:** Static values that shouldn't be configured by end-users (like claim types, internal cookie names, or known identifiers) are stored in `Constants` classes (e.g., `Normora.Shared/Constants/AuthConstants.cs`).

### IOptions Interfaces
* **`IOptions<T>`:** Used for singletons and transient services. It loads the configuration once at startup. We primarily use this.
* **`IOptionsSnapshot<T>`:** Used for scoped services. It recomputes the options on every request, useful if configuration changes dynamically (e.g., updating a `appsettings.json` file without restarting).
* **`IOptionsMonitor<T>`:** Used to receive notifications when configuration changes.

### Configuration Validation
We employ data annotations (e.g., `[Required]`, `[Url]`, `[EmailAddress]`) on our Options classes to ensure configuration integrity. 
These are validated at application startup using `.ValidateDataAnnotations().ValidateOnStart()`. This fail-fast mechanism ensures that missing API keys or invalid endpoints crash the application immediately during startup rather than throwing a null reference exception mid-request in production.

### Modular Ownership and the Shared Project
Currently, infrastructure options (like `GeminiOptions` and `AppOptions`) reside in `Normora.Shared`. This is a deliberate design choice because these dependencies are currently cross-cutting:
* `GeminiOptions` is utilized by both the `Conversations` module (for query rewriting) and the `Normora.Api` host (for embedding and text generation).
* `AppOptions` is used by the `Tenants` module (for invite links) and the `Normora.Api` host.

If strict modular ownership is required in the future, the next architectural refactoring step would involve pushing those API-level service registrations (like `GeminiEmbeddingService` and MinIO/Tika HTTP clients) down into their respective modules (e.g., `Documents` and `Conversations`). Once those features are fully encapsulated within the modules, we can safely migrate the Options classes out of `Normora.Shared` and into their specific module infrastructures.

---

## 2. Configuration Reference

The following options classes are defined in the `Normora.Shared` project. All properties can be overridden by Environment Variables.

### `AppOptions`
* **Section:** `App`
* **Owner:** `Normora.Shared`
* **Properties:**
  * `BaseUrl` (string): The base URL of the frontend SPA (e.g., `http://localhost:4200`). Used for generating email links.

### `KeycloakOptions`
* **Section:** `Keycloak`
* **Owner:** `Normora.Shared`
* **Properties:**
  * `Authority` (string): The primary Keycloak OIDC endpoint (Internal or External depending on environment).
  * `MetadataAddress` (string): The `.well-known/openid-configuration` endpoint.
  * `InternalAuthority` (string): Defaults to `http://keycloak:8080`. Used for server-to-server calls in Docker.
  * `ExternalAuthority` (string): Defaults to `http://localhost:8080`. Used for browser redirects.

### `MinioOptions`
* **Section:** `Minio`
* **Owner:** `Normora.Shared`
* **Properties:**
  * `Endpoint` (string): Host and port for MinIO/S3 API. Defaults to `localhost:9000`.
  * `AccessKey` (string): Sensitive. 
  * `SecretKey` (string): Sensitive.

### `GeminiOptions`
* **Section:** `Gemini`
* **Owner:** `Normora.Shared`
* **Properties:**
  * `ApiKey` (string): Sensitive. The API key for Google Gemini.
  * `Endpoint` (string): Base URL for the Gemini API. Defaults to `https://generativelanguage.googleapis.com/v1beta/`.
  * `EmbeddingModel` (string): Defaults to `gemini-embedding-001`.
  * `GenerationModel` (string): Defaults to `gemini-2.0-flash`.

### `TikaOptions`
* **Section:** `Tika`
* **Owner:** `Normora.Shared`
* **Properties:**
  * `Endpoint` (string): Host for the Apache Tika server. Defaults to `http://localhost:9998/`.

### `SmtpOptions`
* **Section:** `Smtp`
* **Owner:** `Normora.Shared`
* **Properties:**
  * `Host` (string): SMTP Server. Defaults to `localhost`.
  * `Port` (int): SMTP Port. Defaults to `1025` (MailHog).
  * `From` (string): The sender email address. Defaults to `noreply@normora.local`.

---

## 3. Environment Configuration Guide

Normora configures these values differently across environments. ASP.NET Core automatically merges `appsettings.json`, `appsettings.{Environment}.json`, and Environment Variables.

### Overriding with Environment Variables
To override `Keycloak:Authority`, set the environment variable: `Keycloak__Authority`. 
Notice the **double underscore (`__`)** is used to represent the section hierarchy in Linux/Docker.

### Local Development (Bare Metal)
* You generally use `appsettings.Development.json`.
* `Minio:Endpoint` is `localhost:9000`.
* `Keycloak:Authority` is `http://localhost:8080/realms/normora`.

### Docker Compose
* Inside Docker, services communicate using internal hostnames.
* For MinIO: `Minio__Endpoint=minio:9000`
* For Tika: `Tika__Endpoint=http://tika:9998/`
* For Keycloak: The OIDC handler uses `KeycloakOptions.InternalAuthority` (`http://keycloak:8080`) for token validation and backchannel requests, but issues redirects to the browser using `KeycloakOptions.ExternalAuthority` (`http://localhost:8080`).

### Production
* Use Environment Variables for sensitive keys: `Gemini__ApiKey`, `Minio__SecretKey`.
* Ensure `App__BaseUrl` points to the actual HTTPS public domain.
* All HTTP internal endpoints (like Tika/MinIO) are fine, but Keycloak external URLs must be HTTPS.

### Diagnosing Startup Failures
If you see DI resolution errors for `IOptions<T>`, ensure the class is registered in `ConfigurationServiceExtensions.cs`. If values are missing, check if `appsettings.json` is malformed or if environment variables are misspelled.

---

## 4. Constants and Magic Strings

We maintain a strict boundary between what is **configurable** (e.g. URLs, API keys) and what is a **constant** (e.g. claim types, protocols, internal IDs).

The `Normora.Shared/Constants/AuthConstants.cs` file centralizes identity constants:
* **Cookies:** `normora-auth` (Development) and `__Host-spa` (Production).
* **Claims:** Standard mapping keys like `preferred_username` and `role`.
* **Scopes:** `openid`, `profile`, `email`.

These should *never* be configured via `appsettings.json` because changing them would break the application logic and integrations with Keycloak.

---

## 5. Developer Guidance

When you need to introduce new configuration settings, follow these steps:

1. **Create an Options Class:**
   Create a new class in `Normora.Shared/Options/` (or your Module's directory if private).
   ```csharp
   public class PaymentOptions
   {
       public const string SectionName = "Payments";
       public string GatewayUrl { get; set; } = "https://api.stripe.com";
       public string? SecretKey { get; set; }
   }
   ```

2. **Register the Options:**
   Open `Normora.Api/Extensions/ConfigurationServiceExtensions.cs` and add:
   ```csharp
   services.Configure<PaymentOptions>(configuration.GetSection(PaymentOptions.SectionName));
   ```

3. **Inject Options into Services:**
   In your service constructor, inject `IOptions<PaymentOptions>`:
   ```csharp
   public class PaymentService
   {
       private readonly PaymentOptions _options;
       
       public PaymentService(IOptions<PaymentOptions> options)
       {
           _options = options.Value;
       }
   }
   ```

4. **Testing:**
   In unit tests, you can mock `IOptions` using the `Options.Create()` helper:
   ```csharp
   var options = Options.Create(new PaymentOptions { GatewayUrl = "http://test" });
   var service = new PaymentService(options);
   ```

5. **Avoid Magic Strings:**
   If you need a string that represents a database schema name, a fixed policy, or a header key, place it in a `Constants` class, not an options class.

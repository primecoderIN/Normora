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
Shared options (such as `GeminiOptions`, `AppOptions`, and `EmailOptions`) reside in `Normora.Shared` to declare cross-cutting schemas across modules. 

All infrastructure service implementations have been pushed down into their respective modules:
* `GeminiEmbeddingService` and Apache Tika client are encapsulated inside `Normora.Modules.Documents`.
* `GeminiTextGenerationService`, `AgentToolRegistry`, and Agent Tools are encapsulated inside `Normora.Modules.Conversations`.
* `PayUPaymentService` and `TenantResolutionService` are encapsulated inside `Normora.Modules.Tenants`.

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

### `EmailOptions`
* **Section:** `Email`
* **Owner:** `Normora.Shared`
* **Properties:**
  * `Provider` (string): Transactional email delivery provider (`Resend` or `Null`). Defaults to `Resend`.
  * `ApiKey` (string): Sensitive. API key for the transactional email provider. Injected via `RESEND_API_KEY` or `Email__ApiKey`.
  * `ResendApiKey` (string): Alias for backwards compatibility with legacy configuration keys named `ResendApiKey` or `RESEND_API_KEY`.
  * `FromEmail` (string): Sender email address (e.g., `normora@resend.dev` or a verified domain address). Injected via `RESEND_FROM_EMAIL`. Defaults to `onboarding@resend.dev`.
  * `FromName` (string): Friendly display name for the sender. Defaults to `Normora`.
  * `DevOverrideRecipient` (string): Optional developer email address to intercept and redirect all outbound emails during local development and testing. Particularly useful with sandbox providers like Resend's free tier, which restricts delivery strictly to the registered account owner. Injected via `DEV_OVERRIDE_EMAIL`.

### `PayUOptions`
* **Section:** `PayU`
* **Owner:** `Normora.Shared`
* **Properties:**
  * `MerchantKey` (string): PayU Merchant Key identifier (`gtKFFx` in test sandbox). Injected via environment variable `PAYU_MERCHANT_KEY`.
  * `MerchantSalt` (string): Sensitive. PayU Merchant Salt used for generating and verifying SHA-512 cryptographic request and callback signatures. Injected via `PAYU_MERCHANT_SALT`.
  * `IsProduction` (bool): Toggles between PayU sandbox environment (`https://test.payu.in/_payment`) and live production (`https://secure.payu.in/_payment`). Injected via `PAYU_IS_PRODUCTION`. Defaults to `false`.
  * `PaymentUrl` (string, computed): Resolves the active hosted checkout redirect endpoint based on `IsProduction`.

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
* For Redis: `ConnectionStrings__Redis=redis:6379`
* For Resend Email:
  * `Email__ResendApiKey=${RESEND_API_KEY}`
  * `Email__FromEmail=${RESEND_FROM_EMAIL:-normora@resend.dev}`
  * `Email__DevOverrideRecipient=${DEV_OVERRIDE_EMAIL:-}`
* For Keycloak: The OIDC handler uses `KeycloakOptions.InternalAuthority` (`http://keycloak:8080`) for token validation and backchannel requests, but issues redirects to the browser using `KeycloakOptions.ExternalAuthority` (`http://localhost:8080`).
* **Keycloak Memory Setting**: On Windows Docker Desktop, Keycloak is configured with `JAVA_OPTS_APPEND: "-Xms256m -Xmx512m"` to restrict heap usage and prevent unexpected exit code 137 (OOM killed) during concurrent builds.

### Production
* Use Environment Variables for sensitive keys: `Gemini__ApiKey`, `Minio__SecretKey`, `Email__ResendApiKey`, `PayU__MerchantSalt`.
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
       public string GatewayUrl { get; set; } = "https://test.payu.in/_payment";
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

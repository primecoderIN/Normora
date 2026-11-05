# Getting Started

Welcome to the Normora project! This guide will help you set up your local development environment.

## Prerequisites
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (Make sure it's running)
- [Git](https://git-scm.com/)
- PowerShell (Windows) or a Bash terminal (Linux/macOS)

## Spinning Up the Infrastructure

We have configured the entire stack to run locally using Docker Compose. This includes the database, identity provider, object storage, backend API, and frontend client.

1. **Open a terminal** at the root of the repository.
2. **Run the start script**:
   ```powershell
   .\start.ps1
   ```
   *This script will automatically generate a `.env` file with default credentials and start all containers in the background.*
3. **Configure API Keys in `.env`**:
   - **Gemini API Key**: Set `GEMINI_API_KEY` to enable conversational AI, embeddings search, and autonomous tool calling.
   - **Resend Email API Key**: Set `RESEND_API_KEY` to enable transactional email delivery (member invitations, document readiness/failure notices, and HR escalation alerts).
   - **Developer Email Override**: When using Resend's free test tier (`resend.dev`), email delivery is strictly limited to the email address registered with your Resend account. Set `DEV_OVERRIDE_EMAIL=your-resend-account@email.com` so that all simulated outbound emails across all workspaces and roles are safely redirected to your personal inbox with a `[Dev: to {original}]` subject prefix.
4. **Wait for builds**: The first time you run this, Docker will build the .NET API and the Angular client. This might take a few minutes.

## Accessing the Application

Once the containers are up and running, you can access the services at the following URLs:

- **Web Client (Angular)**: [http://localhost:4200](http://localhost:4200)
- **Backend API**: [http://localhost:5000](http://localhost:5000)
- **API Documentation & Testing (Scalar)**: [http://localhost:5000/scalar/v1](http://localhost:5000/scalar/v1)
- **Keycloak Admin Console**: [http://localhost:8080](http://localhost:8080)
- **MinIO Console**: [http://localhost:9001](http://localhost:9001)
- **pgAdmin**: [http://localhost:5050](http://localhost:5050)

## Default Credentials
Check the auto-generated `.env` file in the root directory for default usernames and passwords used in local development (e.g., PostgreSQL, Keycloak, MinIO).

## Stopping the Environment
To stop the containers without destroying your database volumes, run:
```powershell
.\stop.ps1
```

## Troubleshooting Common Setup Issues

### `service 'migrator' didn't complete successfully: exit 139`
If the `migrator` service exits prematurely with exit code `139`:
1. Check the migrator container logs to view the failure:
   ```powershell
   docker compose logs migrator
   ```
2. Ensure your `.env` file exists and that `POSTGRES_USER` and `POSTGRES_PASSWORD` match your database configuration.
3. Verify Docker Desktop / WSL2 has at least 4–6GB of RAM allocated in `%USERPROFILE%\.wslconfig`.
4. Run the migration tool directly on your host machine to isolate local code from container environment issues:
   ```powershell
   dotnet run --project server/Normora.Api/Normora.Api.csproj -- --migrate
   ```
5. For deep-dive architectural and troubleshooting details, see [docs/backend_onboarding.md](file:///c:/Users/Sanjeev%20Kumar/Codes/Normora/docs/backend_onboarding.md#controlled-migrations--the-migrator-service).

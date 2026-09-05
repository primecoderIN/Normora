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
3. **Wait for builds**: The first time you run this, Docker will build the .NET API and the Angular client. This might take a few minutes.

## Accessing the Application

Once the containers are up and running, you can access the services at the following URLs:

- **Web Client (Angular)**: [http://localhost:4200](http://localhost:4200)
- **Backend API**: [http://localhost:5000](http://localhost:5000)
- **API Documentation & Testing (Scalar)**: [http://localhost:5000/scalar/v1](http://localhost:5000/scalar/v1)
- **Keycloak Admin Console**: [http://localhost:8080](http://localhost:8080)
- **MinIO Console**: [http://localhost:9001](http://localhost:9001)
- **Mailpit (Email Testing UI)**: [http://localhost:8025](http://localhost:8025)
- **pgAdmin**: [http://localhost:5050](http://localhost:5050)

## Default Credentials
Check the auto-generated `.env` file in the root directory for default usernames and passwords used in local development (e.g., PostgreSQL, Keycloak, MinIO).

## Stopping the Environment
To stop the containers without destroying your database volumes, run:
```powershell
.\stop.ps1
```

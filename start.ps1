$ErrorActionPreference = "Stop"

$envFile = ".env"
$envContent = @"
POSTGRES_USER=postgres
POSTGRES_PASSWORD=password
POSTGRES_DB=normoradb
KEYCLOAK_ADMIN=admin
KEYCLOAK_ADMIN_PASSWORD=admin
MINIO_ROOT_USER=admin
MINIO_ROOT_PASSWORD=password
ASPNETCORE_ENVIRONMENT=Development
PGADMIN_DEFAULT_EMAIL=admin@normora.com
PGADMIN_DEFAULT_PASSWORD=admin
"@

if (-not (Test-Path $envFile)) {
    Write-Host "Creating .env file with default values..." -ForegroundColor Green
    $envContent | Out-File -FilePath $envFile -Encoding utf8
} else {
    Write-Host ".env file already exists. Skipping creation..." -ForegroundColor Yellow
}

Write-Host "Starting all services with docker-compose..." -ForegroundColor Green
docker-compose up -d --build

Write-Host "Services started successfully!" -ForegroundColor Green

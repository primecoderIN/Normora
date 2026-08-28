$ErrorActionPreference = "Stop"

Write-Host "Stopping all services with docker-compose..." -ForegroundColor Green
docker-compose down

Write-Host "Services stopped successfully!" -ForegroundColor Green

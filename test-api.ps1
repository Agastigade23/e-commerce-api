#!/usr/bin/env pwsh
# Test API startup

cd D:\Projects\API\API

Write-Host "Building API..." -ForegroundColor Cyan
dotnet build --configuration Debug

Write-Host "`nStarting API..." -ForegroundColor Cyan
$env:ASPNETCORE_ENVIRONMENT = "Development"

# Run the API and capture output
dotnet run --no-build 2>&1 | Tee-Object -FilePath api.log &
$apiPid = $?

Write-Host "API starting in background..." -ForegroundColor Green
Start-Sleep -Seconds 10

Write-Host "Testing API health..." -ForegroundColor Cyan
try {
	$response = Invoke-WebRequest -Uri "http://localhost:5103/swagger/index.html" -ErrorAction SilentlyContinue
	if ($response.StatusCode -eq 200) {
		Write-Host "✅ API is running!" -ForegroundColor Green
	}
} catch {
	Write-Host "❌ API health check failed" -ForegroundColor Red
	Write-Host "Check api.log for details"
}

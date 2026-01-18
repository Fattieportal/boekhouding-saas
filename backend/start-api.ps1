# Start API in background
Set-Location C:\Users\Gslik\OneDrive\Documents\boekhouding-saas\backend
Write-Host "Starting API on http://localhost:5001..." -ForegroundColor Cyan
dotnet run --project src/Api

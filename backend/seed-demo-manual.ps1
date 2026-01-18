# Manual Demo Data Seeding Script
$ErrorActionPreference = "Stop"

Write-Host "Manual Demo Seeding via API call" -ForegroundColor Cyan

# Trigger seeding endpoint (if exists) OR manually create chart of accounts
$baseUrl = "http://localhost:5001/api"

# Login
$body = @{ email = "admin@demo.local"; password = "Admin123!" } | ConvertTo-Json
$response = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -Body $body -ContentType "application/json"
$token = $response.token

# Get tenant
$tenant = Invoke-RestMethod -Uri "$baseUrl/tenants/my" -Method Get -Headers @{ "Authorization" = "Bearer $token" }
$headers = @{ "Authorization" = "Bearer $token"; "X-Tenant-Id" = $tenant.id }

Write-Host "Tenant: $($tenant.name)" -ForegroundColor Gray

# Create basic chart of accounts manually
$accountsToCreate = @(
    @{ code = "1000"; name = "Debiteuren"; type = 1 },  # Asset
    @{ code = "1010"; name = "Bank"; type = 1 },        # Asset
    @{ code = "2000"; name = "Crediteuren"; type = 2 }, # Liability
    @{ code = "2010"; name = "BTW te betalen"; type = 2 }, # Liability
    @{ code = "3000"; name = "Eigen Vermogen"; type = 3 }, # Equity
    @{ code = "8000"; name = "Omzet diensten"; type = 4 }, # Revenue ← IMPORTANT!
    @{ code = "4000"; name = "Kostenplaats"; type = 5 }    # Expense
)

foreach ($account in $accountsToCreate) {
    try {
        $accountBody = $account | ConvertTo-Json
        $created = Invoke-RestMethod -Uri "$baseUrl/accounts" `
            -Method Post `
            -Body $accountBody `
            -ContentType "application/json" `
            -Headers $headers
        
        Write-Host "  ✅ Created: $($account.code) - $($account.name)" -ForegroundColor Green
    }
    catch {
        Write-Host "  ⚠️  Failed: $($account.code) - $_" -ForegroundColor Yellow
    }
}

# Verify
$accounts = Invoke-RestMethod -Uri "$baseUrl/accounts" -Method Get -Headers $headers
Write-Host ""
Write-Host "Total accounts created: $($accounts.Count)" -ForegroundColor Cyan
$accounts | Format-Table code, name, type -AutoSize

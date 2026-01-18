# Simple Test Script voor Accounts & Journals API
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Accounts & Journals API Test" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "http://localhost:5001/api"

# 1. Login
Write-Host "1. Login..." -ForegroundColor Yellow
$loginBody = @{
    email = "accountant@local.test"
    password = "Accountant123!"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    
    $token = $loginResponse.token
    Write-Host "Login succesvol" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "Login gefaald: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# 2. Get user's tenants
Write-Host "2. Ophalen user tenants..." -ForegroundColor Yellow
try {
    $authHeaders = @{
        "Authorization" = "Bearer $token"
    }
    $tenantsResponse = Invoke-RestMethod -Uri "$baseUrl/tenants/my" -Method Get -Headers $authHeaders
    
    if ($tenantsResponse.Count -eq 0) {
        Write-Host "Geen tenants gevonden voor deze user!" -ForegroundColor Red
        exit 1
    }
    
    $tenantId = $tenantsResponse[0].id
    Write-Host "Gebruiken tenant: $($tenantsResponse[0].name) ($tenantId)" -ForegroundColor Green
    Write-Host ""
    
    # Setup headers met tenant
    $headers = @{
        "Authorization" = "Bearer $token"
        "X-Tenant-Id" = $tenantId
    }
} catch {
    Write-Host "Fout bij ophalen tenants: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# 3. Get Accounts
Write-Host "3. Ophalen accounts..." -ForegroundColor Yellow
try {
    $accounts = Invoke-RestMethod -Uri "$baseUrl/accounts" -Method Get -Headers $headers
    Write-Host "Succesvol - Totaal: $($accounts.totalCount) accounts" -ForegroundColor Green
    $accounts.items | Format-Table Code, Name, TypeName, IsActive -AutoSize
    Write-Host ""
} catch {
    Write-Host "Fout: $($_.Exception.Message)" -ForegroundColor Red
}

# 4. Get Journals  
Write-Host "4. Ophalen journals..." -ForegroundColor Yellow
try {
    $journals = Invoke-RestMethod -Uri "$baseUrl/journals" -Method Get -Headers $headers
    Write-Host "Succesvol - Totaal: $($journals.totalCount) journals" -ForegroundColor Green
    $journals.items | Format-Table Code, Name, TypeName -AutoSize
    Write-Host ""
} catch {
    Write-Host "Fout: $($_.Exception.Message)" -ForegroundColor Red
}

# 5. Get account by code
Write-Host "5. Ophalen account '1100' (Debiteuren)..." -ForegroundColor Yellow
try {
    $account = Invoke-RestMethod -Uri "$baseUrl/accounts/by-code/1100" -Method Get -Headers $headers
    Write-Host "Gevonden:" -ForegroundColor Green
    $account | Format-List Code, Name, TypeName, IsActive
} catch {
    Write-Host "Fout: $($_.Exception.Message)" -ForegroundColor Red
}

# 6. Filter accounts by type
Write-Host "6. Filter accounts op type 'Asset'..." -ForegroundColor Yellow
try {
    $assetAccounts = Invoke-RestMethod -Uri "$baseUrl/accounts?type=1" -Method Get -Headers $headers
    Write-Host "Gevonden: $($assetAccounts.totalCount) assets" -ForegroundColor Green
    $assetAccounts.items | Format-Table Code, Name -AutoSize
    Write-Host ""
} catch {
    Write-Host "Fout: $($_.Exception.Message)" -ForegroundColor Red
}

# 7. Create new account
Write-Host "7. Aanmaken nieuwe account '7000 - Kantoorkosten'..." -ForegroundColor Yellow
$newAccount = @{
    code = "7000"
    name = "Kantoorkosten"
    type = 5
    isActive = $true
} | ConvertTo-Json

try {
    $created = Invoke-RestMethod -Uri "$baseUrl/accounts" -Method Post -Body $newAccount -Headers $headers -ContentType "application/json"
    Write-Host "Account aangemaakt met ID: $($created.id)" -ForegroundColor Green
    Write-Host ""
    
    # 8. Update the account
    Write-Host "8. Bijwerken account 7000..." -ForegroundColor Yellow
    $updateAccount = @{
        code = "7000"
        name = "Algemene kantoorkosten"
        type = 5
        isActive = $true
    } | ConvertTo-Json
    
    $updated = Invoke-RestMethod -Uri "$baseUrl/accounts/$($created.id)" -Method Put -Body $updateAccount -Headers $headers -ContentType "application/json"
    Write-Host "Account bijgewerkt - Nieuwe naam: $($updated.name)" -ForegroundColor Green
    Write-Host ""
    
    # 9. Delete the account
    Write-Host "9. Verwijderen test account..." -ForegroundColor Yellow
    Invoke-RestMethod -Uri "$baseUrl/accounts/$($created.id)" -Method Delete -Headers $headers | Out-Null
    Write-Host "Account verwijderd" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "Fout: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        $errorDetail = $_.ErrorDetails.Message | ConvertFrom-Json
        Write-Host "   Details: $($errorDetail.message)" -ForegroundColor Red
    }
    Write-Host ""
}

# 10. Test duplicate account code
Write-Host "10. Test dubbele account code (moet falen)..." -ForegroundColor Yellow
$duplicateAccount = @{
    code = "1100"
    name = "Duplicate Test"
    type = 1
    isActive = $true
} | ConvertTo-Json

try {
    Invoke-RestMethod -Uri "$baseUrl/accounts" -Method Post -Body $duplicateAccount -Headers $headers -ContentType "application/json" | Out-Null
    Write-Host "FOUT: Dubbele code werd geaccepteerd!" -ForegroundColor Red
} catch {
    if ($_.ErrorDetails.Message) {
        $errorDetail = $_.ErrorDetails.Message | ConvertFrom-Json
        Write-Host "Correct afgewezen: $($errorDetail.message)" -ForegroundColor Green
    } else {
        Write-Host "Correct afgewezen" -ForegroundColor Green
    }
}
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Test voltooid!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
